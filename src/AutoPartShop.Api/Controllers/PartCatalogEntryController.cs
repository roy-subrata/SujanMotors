using AutoPartShop.Api.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/parts/{partId:guid}/catalog-entry")]
[ApiController]
[Produces("application/json")]
public class PartCatalogEntryController(
    AutoPartDbContext _db,
    ICurrentUserService _currentUserService,
    ILogger<PartCatalogEntryController> _logger) : ControllerBase
{
    // GET /api/parts/{partId}/catalog-entry
    [HttpGet]
    public async Task<IActionResult> Get(Guid partId, CancellationToken ct)
    {
        if (!await _db.Parts.AnyAsync(p => p.Id == partId, ct))
            return NotFound(new { message = "Part not found" });

        var entry = await _db.ProductCatalogEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PartId == partId, ct);

        if (entry is null) return Ok(null);
        return Ok(MapEntry(entry));
    }

    // PUT /api/parts/{partId}/catalog-entry
    [HttpPut]
    public async Task<IActionResult> Upsert(Guid partId, [FromBody] UpsertCatalogEntryRequest req, CancellationToken ct)
    {
        if (!await _db.Parts.AnyAsync(p => p.Id == partId, ct))
            return NotFound(new { message = "Part not found" });

        // Normalise slug: fall back to partId if blank
        var slug = string.IsNullOrWhiteSpace(req.Slug)
            ? partId.ToString()
            : req.Slug.Trim().ToLowerInvariant().Replace(" ", "-");

        // Slug uniqueness check (exclude self)
        if (await _db.ProductCatalogEntries.AnyAsync(
                e => e.Slug == slug && e.PartId != partId, ct))
            return Conflict(new { message = $"Slug '{slug}' is already used by another product" });

        var existing = await _db.ProductCatalogEntries
            .FirstOrDefaultAsync(e => e.PartId == partId, ct);

        var user = _currentUserService.GetCurrentUsername();

        if (existing is null)
        {
            var entry = ProductCatalogEntry.Create(
                partId, slug,
                shortDescription: req.ShortDescription ?? "",
                isPublished: req.IsPublished,
                publishedAt: req.IsPublished ? DateTime.UtcNow : null,
                isFeatured: req.IsFeatured,
                featuredRank: req.FeaturedRank,
                popularityScore: 0,
                primaryImageUrl: "",
                videoUrl: null,
                metaTitle: req.MetaTitle,
                metaDescription: req.MetaDescription);

            entry.CreatedBy = user;
            entry.ModifiedBy = user;
            _db.ProductCatalogEntries.Add(entry);
            await _db.SaveChangesAsync(ct);
            return Ok(MapEntry(entry));
        }

        existing.UpdateListing(
            slug,
            shortDescription: req.ShortDescription ?? "",
            isPublished: req.IsPublished,
            publishedAt: req.IsPublished ? (existing.PublishedAt ?? DateTime.UtcNow) : null,
            isFeatured: req.IsFeatured,
            featuredRank: req.FeaturedRank,
            popularityScore: existing.PopularityScore,
            primaryImageUrl: existing.PrimaryImageUrl,
            videoUrl: existing.VideoUrl,
            metaTitle: req.MetaTitle,
            metaDescription: req.MetaDescription);

        existing.ModifiedBy = user;
        await _db.SaveChangesAsync(ct);
        return Ok(MapEntry(existing));
    }

    private static object MapEntry(ProductCatalogEntry e) => new
    {
        e.PartId,
        e.Slug,
        e.ShortDescription,
        e.IsPublished,
        e.PublishedAt,
        e.IsFeatured,
        e.FeaturedRank,
        e.MetaTitle,
        e.MetaDescription
    };
}

public class UpsertCatalogEntryRequest
{
    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int FeaturedRank { get; set; } = 0;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}
