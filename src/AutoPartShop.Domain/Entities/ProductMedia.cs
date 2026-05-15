namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Media assets for products and variants (images/videos).
/// </summary>
public class ProductMedia : AuditableEntity
{
    public Guid PartId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string MediaType { get; private set; } = "image"; // image, video
    public string? AltText { get; private set; }              // SEO / accessibility alt text
    public string? FileName { get; private set; }             // Original file name (useful for upload management)
    public int SortOrder { get; private set; } = 0;
    public bool IsPrimary { get; private set; } = false;

    public Part? Part { get; set; }
    public ProductVariant? Variant { get; set; }

    private ProductMedia() { }

    public static ProductMedia Create(
        Guid partId,
        string url,
        string mediaType = "image",
        int sortOrder = 0,
        bool isPrimary = false,
        Guid? variantId = null,
        string? altText = null,
        string? fileName = null)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty", nameof(url));

        return new ProductMedia
        {
            PartId = partId,
            VariantId = variantId,
            Url = url.Trim(),
            MediaType = string.IsNullOrWhiteSpace(mediaType) ? "image" : mediaType.Trim().ToLowerInvariant(),
            AltText = altText?.Trim(),
            FileName = fileName?.Trim(),
            SortOrder = sortOrder < 0 ? 0 : sortOrder,
            IsPrimary = isPrimary
        };
    }

    public void Update(string url, string mediaType, int sortOrder, bool isPrimary, Guid? variantId,
        string? altText = null, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty", nameof(url));

        Url = url.Trim();
        MediaType = string.IsNullOrWhiteSpace(mediaType) ? "image" : mediaType.Trim().ToLowerInvariant();
        AltText = altText?.Trim();
        FileName = fileName?.Trim();
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
        IsPrimary = isPrimary;
        VariantId = variantId;
    }
}
