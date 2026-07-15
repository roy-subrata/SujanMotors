namespace AutoPartShop.Application.DTOs.BackupDtos;

/// <summary>
/// A single backup history entry
/// </summary>
public class BackupRecordDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public bool UploadedToDrive { get; set; }
    public bool LocalFileExists { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Result of validating the Google Drive connection/folder
/// </summary>
public class DriveStatusResult
{
    /// <summary>Whether a service-account credential is configured on the server</summary>
    public bool Configured { get; set; }

    /// <summary>Whether the shared folder is reachable and listable</summary>
    public bool Ok { get; set; }

    /// <summary>Service account email the admin must share the Drive folder with</summary>
    public string? ServiceAccountEmail { get; set; }

    public string? Error { get; set; }
}

/// <summary>
/// Request body for restoring a backup — confirmation must be the literal string "RESTORE"
/// </summary>
public class RestoreBackupRequest
{
    public string Confirmation { get; set; } = string.Empty;
}
