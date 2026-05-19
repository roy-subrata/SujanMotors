namespace AutoPartShop.Domain.Entities;

/// <summary>
/// E-commerce specific listing metadata for a part.
/// Keeps POS data untouched while enabling storefront features.
/// </summary>
public class ProductCatalogEntry : AuditableEntity
{
    public Guid PartId { get; private set; }
    public string Slug { get; private set; } = string.Empty;       // URL-friendly identifier
    public string ShortDescription { get; private set; } = string.Empty;  // Listing card summary
    public bool IsPublished { get; private set; } = true;
    public DateTime? PublishedAt { get; private set; }
    public bool IsFeatured { get; private set; } = false;
    public int FeaturedRank { get; private set; } = 0;
    public decimal PopularityScore { get; private set; } = 0;
    public string PrimaryImageUrl { get; private set; } = string.Empty;
    public string? VideoUrl { get; private set; }          // Product demo / promo video

    // SEO
    public string? MetaTitle { get; private set; }         // <title> tag override
    public string? MetaDescription { get; private set; }  // <meta name="description"> tag

    public Part? Part { get; set; }

    private ProductCatalogEntry() { }

    public static ProductCatalogEntry Create(
        Guid partId,
        string slug,
        string shortDescription = "",
        bool isPublished = true,
        DateTime? publishedAt = null,
        bool isFeatured = false,
        int featuredRank = 0,
        decimal popularityScore = 0,
        string primaryImageUrl = "",
        string? videoUrl = null,
        string? metaTitle = null,
        string? metaDescription = null)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty", nameof(slug));

        return new ProductCatalogEntry
        {
            PartId = partId,
            Slug = slug.Trim().ToLowerInvariant(),
            ShortDescription = shortDescription?.Trim() ?? string.Empty,
            IsPublished = isPublished,
            PublishedAt = publishedAt,
            IsFeatured = isFeatured,
            FeaturedRank = featuredRank < 0 ? 0 : featuredRank,
            PopularityScore = popularityScore < 0 ? 0 : popularityScore,
            PrimaryImageUrl = primaryImageUrl?.Trim() ?? string.Empty,
            VideoUrl = videoUrl?.Trim(),
            MetaTitle = metaTitle?.Trim(),
            MetaDescription = metaDescription?.Trim()
        };
    }

    public void UpdateListing(
        string slug,
        string shortDescription,
        bool isPublished,
        DateTime? publishedAt,
        bool isFeatured,
        int featuredRank,
        decimal popularityScore,
        string primaryImageUrl,
        string? videoUrl = null,
        string? metaTitle = null,
        string? metaDescription = null)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty", nameof(slug));

        Slug = slug.Trim().ToLowerInvariant();
        ShortDescription = shortDescription?.Trim() ?? string.Empty;
        IsPublished = isPublished;
        PublishedAt = publishedAt;
        IsFeatured = isFeatured;
        FeaturedRank = featuredRank < 0 ? 0 : featuredRank;
        PopularityScore = popularityScore < 0 ? 0 : popularityScore;
        PrimaryImageUrl = primaryImageUrl?.Trim() ?? string.Empty;
        VideoUrl = videoUrl?.Trim();
        MetaTitle = metaTitle?.Trim();
        MetaDescription = metaDescription?.Trim();
    }
}
