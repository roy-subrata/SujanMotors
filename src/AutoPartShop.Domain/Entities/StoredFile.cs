namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Record of an uploaded binary (image / video / document) kept in blob storage.
/// The actual bytes live behind IFileStorageService (local disk today, S3-compatible later);
/// StorageKey is the provider-relative path. Optionally tagged with the owning entity
/// (e.g. PRODUCT / EMPLOYEE) so attachments can be listed per record.
/// </summary>
public class StoredFile : AuditableEntity
{
    public string StorageKey { get; private set; } = string.Empty;       // e.g. "products/2026/07/{guid}.jpg"
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Kind { get; private set; } = "IMAGE";     // IMAGE, VIDEO, DOCUMENT
    public string OwnerType { get; private set; } = string.Empty;  // PRODUCT, EMPLOYEE, ... ("" = unattached)
    public Guid? OwnerId { get; private set; }
    public bool IsPublic { get; private set; }  // Public files are served without authentication (unguessable GUID key)

    private StoredFile() { }

    public static StoredFile Create(
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string kind,
        bool isPublic,
        string ownerType = "",
        Guid? ownerId = null)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("StorageKey cannot be empty", nameof(storageKey));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("OriginalFileName cannot be empty", nameof(originalFileName));

        if (sizeBytes <= 0)
            throw new ArgumentException("SizeBytes must be positive", nameof(sizeBytes));

        var validKinds = new[] { "IMAGE", "VIDEO", "DOCUMENT" };
        var resolvedKind = kind?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!validKinds.Contains(resolvedKind))
            throw new ArgumentException("Kind must be IMAGE, VIDEO, or DOCUMENT", nameof(kind));

        return new StoredFile
        {
            StorageKey = storageKey.Trim(),
            OriginalFileName = originalFileName.Trim(),
            ContentType = contentType?.Trim() ?? "application/octet-stream",
            SizeBytes = sizeBytes,
            Kind = resolvedKind,
            IsPublic = isPublic,
            OwnerType = ownerType?.Trim().ToUpperInvariant() ?? string.Empty,
            OwnerId = ownerId
        };
    }

    public void AssignOwner(string ownerType, Guid? ownerId)
    {
        OwnerType = ownerType?.Trim().ToUpperInvariant() ?? string.Empty;
        OwnerId = ownerId;
    }
}
