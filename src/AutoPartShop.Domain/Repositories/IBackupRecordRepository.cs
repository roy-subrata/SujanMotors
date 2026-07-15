namespace AutoPartShop.Domain.Repositories;

using AutoPartShop.Domain.Entities;

/// <summary>
/// Repository interface for BackupRecord entity
/// </summary>
public interface IBackupRecordRepository : IBaseRepository<BackupRecord>
{
    /// <summary>
    /// Get backup history ordered by StartedAt descending
    /// </summary>
    Task<(List<BackupRecord> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get restorable backups (Succeeded/UploadFailed) beyond the newest <paramref name="keepCount"/>,
    /// oldest first — candidates for retention pruning
    /// </summary>
    Task<List<BackupRecord>> GetRestorableBeyondRetentionAsync(
        int keepCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a scheduled backup has started since the given UTC cutoff (used to run at most once per day)
    /// </summary>
    Task<bool> HasScheduledRunSinceAsync(DateTime utcCutoff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records still Pending/Running — after an app restart these are orphans of interrupted runs
    /// </summary>
    Task<List<BackupRecord>> GetInProgressAsync(CancellationToken cancellationToken = default);
}
