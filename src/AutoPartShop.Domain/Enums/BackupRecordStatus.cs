namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.BackupRecord"/>.
/// Member names are serialized as-is on the wire (global JsonStringEnumConverter, no naming
/// policy) — they must match the historical string literals exactly.
/// </summary>
public enum BackupRecordStatus
{
    Pending,
    Running,
    Succeeded,
    UploadFailed,
    Failed
}
