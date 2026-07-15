namespace AutoPartShop.Application.DTOs.FileDtos;

public class StoredFileDto
{
    public Guid Id { get; set; }
    /// <summary>URL the file is served from (relative unless FileStorage:PublicBaseUrl is configured).</summary>
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Kind { get; set; } = string.Empty;       // IMAGE, VIDEO, DOCUMENT
    public bool IsPublic { get; set; }
    public string OwnerType { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
}
