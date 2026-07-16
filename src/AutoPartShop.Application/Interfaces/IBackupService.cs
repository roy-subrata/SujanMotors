namespace AutoPartShop.Application.Interfaces;

using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.BackupDtos;

/// <summary>
/// Database backup and restore operations
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Run the full backup pipeline for an already-created record: BACKUP DATABASE → verify →
    /// upload to cloud (if configured) → retention. Never throws; failures are recorded on the record.
    /// </summary>
    Task ExecuteBackupAsync(Guid backupRecordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new backup record in Pending state. Pair with <see cref="ExecuteBackupAsync"/>.
    /// </summary>
    Task<Guid> CreateBackupRecordAsync(string triggerType, string initiatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore the given backup over the live database. Takes a safety backup first.
    /// Throws on failure.
    /// </summary>
    Task RestoreAsync(Guid backupRecordId, string initiatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paged backup history, newest first.
    /// </summary>
    Task<PagedResult<BackupRecordDto>> GetHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure the backup file exists locally (downloading from cloud if needed) and return its path.
    /// </summary>
    Task<(string FilePath, string FileName)> GetBackupFileAsync(Guid backupRecordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate the cloud storage configuration using the folder id from settings.
    /// </summary>
    Task<DriveStatusResult> GetDriveStatusAsync(CancellationToken cancellationToken = default);
}
