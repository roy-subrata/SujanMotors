namespace AutoPartShop.Infrastructure.Services.Backup;

using System.Text.RegularExpressions;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.BackupDtos;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Backup/restore via native SQL Server BACKUP/RESTORE T-SQL.
///
/// Backups are written to SQL Server's own default backup directory (InstanceDefaultBackupPath,
/// container-local) and then pulled over the SQL connection via OPENROWSET(BULK ... SINGLE_BLOB)
/// into Backup:Directory on the API side. SQL Server cannot BACKUP directly onto a Docker Desktop
/// (Windows) bind mount — the write fails with OS error 31 on DiskChangeFileSize — but *reading*
/// from the mount works everywhere, so restores go the other way: the API places the .bak in the
/// shared folder and SQL Server reads it via Backup:SqlServerDirectory (the same folder as SQL
/// Server sees it).
/// </summary>
public sealed class SqlServerBackupService(
    IConfiguration configuration,
    IBackupRecordRepository backupRepository,
    IApplicationSettingsRepository settingsRepository,
    IBackupStorage backupStorage,
    BackupCoordinator coordinator,
    AutoPartDbContext dbContext,
    ILogger<SqlServerBackupService> logger) : IBackupService
{
    public const string SettingFolderIdKey = "BACKUP:GDRIVE_FOLDER_ID";
    public const string SettingRetentionCountKey = "BACKUP:RETENTION_COUNT";

    private static readonly TimeSpan[] UploadRetryDelays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(45)];

    private readonly IConfiguration _configuration = configuration;
    private readonly IBackupRecordRepository _backupRepository = backupRepository;
    private readonly IApplicationSettingsRepository _settingsRepository = settingsRepository;
    private readonly IBackupStorage _backupStorage = backupStorage;
    private readonly BackupCoordinator _coordinator = coordinator;
    private readonly AutoPartDbContext _dbContext = dbContext;
    private readonly ILogger<SqlServerBackupService> _logger = logger;

    /// <inheritdoc/>
    public async Task<Guid> CreateBackupRecordAsync(string triggerType, string initiatedBy, CancellationToken cancellationToken = default)
    {
        var fileName = $"{GetDatabaseName()}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
        var record = BackupRecord.Create(fileName, triggerType, initiatedBy);
        await _backupRepository.AddAsync(record, cancellationToken);
        return record.Id;
    }

    /// <inheritdoc/>
    public async Task ExecuteBackupAsync(Guid backupRecordId, CancellationToken cancellationToken = default)
    {
        var record = await _backupRepository.GetByIdAsync(backupRecordId, cancellationToken)
            ?? throw new InvalidOperationException($"Backup record {backupRecordId} not found");

        if (!_coordinator.TryBegin("backup"))
        {
            record.MarkFailed($"Another operation ({_coordinator.CurrentOperation}) is already in progress.");
            await _backupRepository.UpdateAsync(record, cancellationToken);
            return;
        }

        try
        {
            await RunBackupPipelineAsync(record, cancellationToken);
        }
        finally
        {
            _coordinator.End();
        }
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(Guid backupRecordId, string initiatedBy, CancellationToken cancellationToken = default)
    {
        var record = await _backupRepository.GetByIdAsync(backupRecordId, cancellationToken)
            ?? throw new InvalidOperationException($"Backup record {backupRecordId} not found");

        if (!record.IsRestorable)
            throw new InvalidOperationException($"Backup '{record.FileName}' is not restorable (status: {record.Status}).");

        if (!_coordinator.TryBegin("restore"))
            throw new InvalidOperationException($"Another operation ({_coordinator.CurrentOperation}) is already in progress.");

        try
        {
            // Ensure the .bak exists locally before touching the live database
            await EnsureLocalFileAsync(record, cancellationToken);

            // Safety backup of the current state — abort the restore if it fails
            var safetyRecord = BackupRecord.Create(
                $"{GetDatabaseName()}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_prerestore.bak",
                BackupRecord.TriggerTypes.PreRestore,
                initiatedBy);
            await _backupRepository.AddAsync(safetyRecord, cancellationToken);
            await RunBackupPipelineAsync(safetyRecord, cancellationToken);

            var refreshedSafety = await _backupRepository.GetByIdAsync(safetyRecord.Id, cancellationToken);
            if (refreshedSafety is null || !refreshedSafety.IsRestorable)
                throw new InvalidOperationException(
                    $"Pre-restore safety backup failed ({refreshedSafety?.ErrorMessage ?? "unknown error"}); restore aborted.");

            _logger.LogWarning("Restoring database from backup {FileName} (initiated by {User})",
                record.FileName, initiatedBy);

            await RestoreDatabaseAsync(record.FileName, cancellationToken);

            // Roll the restored schema forward in case the backup predates newer migrations
            SqlConnection.ClearAllPools();
            await _dbContext.Database.MigrateAsync(cancellationToken);

            await ReconcileHistoryAfterRestoreAsync(record, refreshedSafety);

            _logger.LogWarning("Database restore from {FileName} completed successfully", record.FileName);
        }
        finally
        {
            _coordinator.End();
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<BackupRecordDto>> GetHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _backupRepository.GetPagedAsync(page, pageSize, cancellationToken);
        var directory = GetApiDirectory(requireExists: false);

        var dtos = items.Select(b => new BackupRecordDto
        {
            Id = b.Id,
            FileName = b.FileName,
            SizeBytes = b.SizeBytes,
            Status = b.Status,
            TriggerType = b.TriggerType,
            UploadedToDrive = b.GoogleDriveFileId != null,
            LocalFileExists = directory != null && File.Exists(Path.Combine(directory, b.FileName)),
            StartedAt = b.StartedAt,
            CompletedAt = b.CompletedAt,
            ErrorMessage = b.ErrorMessage,
            CreatedBy = b.CreatedBy
        }).ToList();

        return PagedResult<BackupRecordDto>.Create(dtos, totalCount, page, pageSize);
    }

    /// <inheritdoc/>
    public async Task<(string FilePath, string FileName)> GetBackupFileAsync(Guid backupRecordId, CancellationToken cancellationToken = default)
    {
        var record = await _backupRepository.GetByIdAsync(backupRecordId, cancellationToken)
            ?? throw new InvalidOperationException($"Backup record {backupRecordId} not found");

        if (!record.IsRestorable)
            throw new InvalidOperationException($"Backup '{record.FileName}' has no file (status: {record.Status}).");

        return await EnsureLocalFileAsync(record, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DriveStatusResult> GetDriveStatusAsync(CancellationToken cancellationToken = default)
    {
        var folderId = await _settingsRepository.GetValueAsync(SettingFolderIdKey, cancellationToken);
        return await _backupStorage.ValidateAsync(folderId, cancellationToken);
    }

    // ---------------------------------------------------------------------
    // Backup pipeline
    // ---------------------------------------------------------------------

    private async Task RunBackupPipelineAsync(BackupRecord record, CancellationToken cancellationToken)
    {
        try
        {
            record.MarkRunning();
            await _backupRepository.UpdateAsync(record, cancellationToken);

            var apiDirectory = GetApiDirectory(requireExists: true)!;
            var localPath = Path.Combine(apiDirectory, record.FileName);

            await BackupDatabaseAsync(record.FileName, localPath, cancellationToken);

            var sizeBytes = new FileInfo(localPath).Length;

            var folderId = await _settingsRepository.GetValueAsync(SettingFolderIdKey, cancellationToken);
            string? driveFileId = null;

            if (!string.IsNullOrWhiteSpace(folderId))
            {
                try
                {
                    driveFileId = await UploadWithRetryAsync(localPath, record.FileName, folderId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Backup {FileName} completed locally but Google Drive upload failed", record.FileName);
                    record.MarkUploadFailed(sizeBytes, $"Google Drive upload failed: {ex.Message}");
                    await _backupRepository.UpdateAsync(record, cancellationToken);
                    await ApplyRetentionAsync(record, cancellationToken);
                    return;
                }
            }

            record.MarkSucceeded(sizeBytes, driveFileId);
            await _backupRepository.UpdateAsync(record, cancellationToken);
            _logger.LogInformation("Backup {FileName} completed ({SizeBytes} bytes, drive: {Uploaded})",
                record.FileName, sizeBytes, driveFileId != null);

            await ApplyRetentionAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup {FileName} failed", record.FileName);
            record.MarkFailed(ex.Message);
            // Best effort — if even the status update fails there is nothing more to do here
            try
            {
                await _backupRepository.UpdateAsync(record, CancellationToken.None);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to record failure status for backup {FileName}", record.FileName);
            }
        }
    }

    private async Task BackupDatabaseAsync(string fileName, string localPath, CancellationToken cancellationToken)
    {
        var dbName = GetDatabaseName();

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        // SQL Server's own default backup directory (container-local; direct writes onto a
        // Docker Desktop bind mount fail with OS error 31)
        string stagingDir;
        await using (var pathCommand = connection.CreateCommand())
        {
            pathCommand.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000))";
            stagingDir = (string?)await pathCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Could not determine SQL Server's default backup path.");
        }
        var stagingPath = $"{stagingDir.TrimEnd('/', '\\')}/{fileName}";

        try
        {
            await using (var backupCommand = connection.CreateCommand())
            {
                backupCommand.CommandTimeout = 0;
                backupCommand.CommandText =
                    $"BACKUP DATABASE [{dbName}] TO DISK = @path WITH INIT, FORMAT, CHECKSUM, COMPRESSION";
                backupCommand.Parameters.AddWithValue("@path", stagingPath);
                await backupCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var verifyCommand = connection.CreateCommand())
            {
                verifyCommand.CommandTimeout = 0;
                verifyCommand.CommandText = "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM";
                verifyCommand.Parameters.AddWithValue("@path", stagingPath);
                await verifyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await PullBackupFileAsync(connection, stagingPath, localPath, cancellationToken);
        }
        finally
        {
            // Remove the staged copy so the SQL data volume only ever holds the backup in flight
            try
            {
                await using var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "EXEC master.sys.xp_delete_files @path";
                deleteCommand.Parameters.AddWithValue("@path", stagingPath);
                await deleteCommand.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete staged backup file {Path} on the SQL Server side", stagingPath);
            }
        }
    }

    /// <summary>
    /// Stream the staged .bak from the SQL Server host to the API side over the SQL connection.
    /// </summary>
    private static async Task PullBackupFileAsync(
        SqlConnection connection,
        string stagingPath,
        string localPath,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        // OPENROWSET(BULK ...) requires a literal path — stagingPath is server-generated, not user input
        command.CommandText = $"SELECT BulkColumn FROM OPENROWSET(BULK N'{stagingPath.Replace("'", "''")}', SINGLE_BLOB) AS backup_file";

        await using var reader = await command.ExecuteReaderAsync(
            System.Data.CommandBehavior.SequentialAccess, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException($"Staged backup '{stagingPath}' could not be read back from SQL Server.");

        await using var source = reader.GetStream(0);
        await using var destination = new FileStream(localPath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private async Task<string> UploadWithRetryAsync(
        string localPath,
        string fileName,
        string folderId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await _backupStorage.UploadAsync(localPath, fileName, folderId, cancellationToken);
            }
            catch (Exception ex) when (attempt < UploadRetryDelays.Length)
            {
                _logger.LogWarning(ex,
                    "Google Drive upload attempt {Attempt} for {FileName} failed; retrying in {Delay}",
                    attempt + 1, fileName, UploadRetryDelays[attempt]);
                await Task.Delay(UploadRetryDelays[attempt], cancellationToken);
            }
        }
    }

    private async Task ApplyRetentionAsync(BackupRecord justCompleted, CancellationToken cancellationToken)
    {
        try
        {
            // A pre-restore safety backup runs mid-restore: pruning here could delete the very
            // backup being restored (when it's the oldest retained one). Defer to the next
            // regular backup instead.
            if (justCompleted.TriggerType == BackupRecord.TriggerTypes.PreRestore)
                return;

            var retentionValue = await _settingsRepository.GetValueAsync(SettingRetentionCountKey, cancellationToken);
            if (!int.TryParse(retentionValue, out var keepCount) || keepCount <= 0)
                return;

            var expired = await _backupRepository.GetRestorableBeyondRetentionAsync(keepCount, cancellationToken);
            if (expired.Count == 0)
                return;

            var directory = GetApiDirectory(requireExists: false);

            foreach (var record in expired)
            {
                try
                {
                    if (directory != null)
                    {
                        var path = Path.Combine(directory, record.FileName);
                        if (File.Exists(path))
                            File.Delete(path);
                    }

                    if (record.GoogleDriveFileId != null)
                        await _backupStorage.DeleteAsync(record.GoogleDriveFileId, cancellationToken);

                    record.Prune();
                    await _backupRepository.UpdateAsync(record, cancellationToken);
                    _logger.LogInformation("Pruned expired backup {FileName}", record.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to prune expired backup {FileName}; will retry after the next backup",
                        record.FileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Backup retention pass failed");
        }
    }

    /// <summary>
    /// The restored BackupRecords table reflects the moment the backup was taken: the restored
    /// backup's own record is frozen in "Running" and the pre-restore safety record doesn't exist
    /// yet. Both entities are still tracked in memory with their final values, so write them back.
    /// Best effort — a failure here must not fail an otherwise successful restore.
    /// </summary>
    private async Task ReconcileHistoryAfterRestoreAsync(BackupRecord restoredBackup, BackupRecord safetyBackup)
    {
        try
        {
            // Row exists inside the .bak (in Running state) — overwrite with the final values
            await _backupRepository.UpdateAsync(restoredBackup, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reconcile the restored backup's history record {FileName}",
                restoredBackup.FileName);
        }

        try
        {
            // The safety record was created after the snapshot, so it's absent — re-insert it
            _dbContext.Entry(safetyBackup).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not re-insert the pre-restore safety backup record {FileName}",
                safetyBackup.FileName);
        }
    }

    // ---------------------------------------------------------------------
    // Restore
    // ---------------------------------------------------------------------

    private async Task RestoreDatabaseAsync(string fileName, CancellationToken cancellationToken)
    {
        var dbName = GetDatabaseName();
        var sqlPath = GetSqlServerPath(fileName);

        var builder = new SqlConnectionStringBuilder(GetConnectionString())
        {
            InitialCatalog = "master",
            Pooling = false
        };

        // Kill pooled app connections up front so they don't compete for the single-user slot
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var singleUser = false;
        try
        {
            await ExecuteAsync(connection,
                $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", cancellationToken);
            singleUser = true;

            await using (var restoreCommand = connection.CreateCommand())
            {
                restoreCommand.CommandTimeout = 0;
                restoreCommand.CommandText = $"RESTORE DATABASE [{dbName}] FROM DISK = @path WITH REPLACE, CHECKSUM";
                restoreCommand.Parameters.AddWithValue("@path", sqlPath);
                await restoreCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (singleUser)
            {
                try
                {
                    await ExecuteAsync(connection, $"ALTER DATABASE [{dbName}] SET MULTI_USER", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex,
                        "Failed to return database {DbName} to MULTI_USER after restore. " +
                        "Manual recovery required: run 'ALTER DATABASE [{DbName}] SET MULTI_USER' via sqlcmd, " +
                        "or restore the PreRestore safety backup manually.", dbName, dbName);
                }
            }
        }
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // ---------------------------------------------------------------------
    // Paths / config
    // ---------------------------------------------------------------------

    private async Task<(string FilePath, string FileName)> EnsureLocalFileAsync(BackupRecord record, CancellationToken cancellationToken)
    {
        var directory = GetApiDirectory(requireExists: true)!;
        var localPath = Path.Combine(directory, record.FileName);

        if (File.Exists(localPath))
            return (localPath, record.FileName);

        if (record.GoogleDriveFileId == null)
            throw new FileNotFoundException(
                $"Backup file '{record.FileName}' no longer exists locally and was never uploaded to Google Drive.");

        _logger.LogInformation("Backup {FileName} missing locally; downloading from Google Drive", record.FileName);
        await _backupStorage.DownloadAsync(record.GoogleDriveFileId, localPath, cancellationToken);
        return (localPath, record.FileName);
    }

    private string GetConnectionString() =>
        _configuration.GetConnectionString("AutoPartDb")
        ?? throw new InvalidOperationException("Connection string 'AutoPartDb' is not configured.");

    private string GetDatabaseName()
    {
        var dbName = new SqlConnectionStringBuilder(GetConnectionString()).InitialCatalog;
        if (string.IsNullOrWhiteSpace(dbName) || !Regex.IsMatch(dbName, "^[A-Za-z0-9_]+$"))
            throw new InvalidOperationException($"Unsupported database name '{dbName}' for backup operations.");
        return dbName;
    }

    private string? GetApiDirectory(bool requireExists)
    {
        var directory = _configuration["Backup:Directory"];
        if (string.IsNullOrWhiteSpace(directory))
        {
            if (requireExists)
                throw new InvalidOperationException(
                    "Backup:Directory is not configured (the backup folder as seen by the API process).");
            return null;
        }

        if (requireExists)
            Directory.CreateDirectory(directory);

        return directory;
    }

    private string GetSqlServerPath(string fileName)
    {
        var sqlDirectory = _configuration["Backup:SqlServerDirectory"];
        if (string.IsNullOrWhiteSpace(sqlDirectory))
            throw new InvalidOperationException(
                "Backup:SqlServerDirectory is not configured (the backup folder as seen by SQL Server).");

        return $"{sqlDirectory.TrimEnd('/', '\\')}/{fileName}";
    }
}
