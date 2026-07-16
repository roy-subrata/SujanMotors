namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a single database backup run (scheduled, manual, or pre-restore safety backup)
/// </summary>
public sealed class BackupRecord : AuditableEntity
{
    public static class Statuses
    {
        public const string Pending = "Pending";
        public const string Running = "Running";
        public const string Succeeded = "Succeeded";
        public const string UploadFailed = "UploadFailed";
        public const string Failed = "Failed";
    }

    public static class TriggerTypes
    {
        public const string Manual = "Manual";
        public const string Scheduled = "Scheduled";
        public const string PreRestore = "PreRestore";
    }

    /// <summary>
    /// Backup file name (e.g. "AutoPartShopDb_20260714_020000.bak")
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Size of the backup file in bytes (0 until the backup completes)
    /// </summary>
    public long SizeBytes { get; private set; }

    /// <summary>
    /// Pending, Running, Succeeded, UploadFailed (local backup OK but cloud upload failed), Failed
    /// </summary>
    public string Status { get; private set; } = Statuses.Pending;

    /// <summary>
    /// Manual, Scheduled, or PreRestore (automatic safety backup taken before a restore)
    /// </summary>
    public string TriggerType { get; private set; } = TriggerTypes.Manual;

    /// <summary>
    /// Google Drive file id once uploaded; null if upload skipped or failed
    /// </summary>
    public string? GoogleDriveFileId { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public string? ErrorMessage { get; private set; }

    // Private constructor for EF Core
    private BackupRecord() { }

    public static BackupRecord Create(string fileName, string triggerType, string initiatedBy)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Backup file name cannot be empty", nameof(fileName));

        var validTriggers = new[] { TriggerTypes.Manual, TriggerTypes.Scheduled, TriggerTypes.PreRestore };
        if (!validTriggers.Contains(triggerType))
            throw new ArgumentException($"Trigger type must be one of: {string.Join(", ", validTriggers)}", nameof(triggerType));

        return new BackupRecord
        {
            FileName = fileName.Trim(),
            TriggerType = triggerType,
            Status = Statuses.Pending,
            StartedAt = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedBy = initiatedBy,
            ModifiedBy = initiatedBy,
            Isdeleted = false
        };
    }

    public void MarkRunning()
    {
        Status = Statuses.Running;
        StartedAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public void MarkSucceeded(long sizeBytes, string? googleDriveFileId)
    {
        Status = Statuses.Succeeded;
        SizeBytes = sizeBytes;
        GoogleDriveFileId = googleDriveFileId;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = null;
        ModifiedDate = DateTime.UtcNow;
    }

    public void MarkUploadFailed(long sizeBytes, string error)
    {
        Status = Statuses.UploadFailed;
        SizeBytes = sizeBytes;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = Truncate(error);
        ModifiedDate = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = Statuses.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = Truncate(error);
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft-delete the record once its files have been removed by retention
    /// </summary>
    public void Prune()
    {
        Isdeleted = true;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// True when the backup produced a usable .bak file (locally and/or on Drive)
    /// </summary>
    public bool IsRestorable => Status is Statuses.Succeeded or Statuses.UploadFailed;

    private static string Truncate(string error) =>
        error.Length <= 2000 ? error : error[..2000];
}
