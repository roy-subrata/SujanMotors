using AutoPartShop.Api.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/parts/{partId:guid}/variants")]
[ApiController]
[Produces("application/json")]
public class ProductVariantController : ControllerBase
{
    private readonly AutoPartDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProductVariantController> _logger;
    private readonly IProductVariantPriceHistoryRepository _priceHistoryRepository;

    public ProductVariantController(
        AutoPartDbContext db,
        ICurrentUserService currentUserService,
        ILogger<ProductVariantController> logger,
        IProductVariantPriceHistoryRepository priceHistoryRepository)
    {
        _db = db;
        _currentUserService = currentUserService;
        _logger = logger;
        _priceHistoryRepository = priceHistoryRepository;
    }

    // GET /api/parts/{partId}/variants
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid partId, CancellationToken ct)
    {
        if (!await _db.Parts.AnyAsync(p => p.Id == partId, ct))
            return NotFound(new { message = "Product not found" });

        var variants = await _db.ProductVariants
            .Where(v => v.PartId == partId)
            .Include(v => v.Attributes)
                .ThenInclude(av => av.Attribute)
            .Include(v => v.Attributes)
                .ThenInclude(av => av.Option)
            .OrderBy(v => v.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(variants.Select(MapVariant));
    }

    // GET /api/parts/{partId}/variants/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid partId, Guid id, CancellationToken ct)
    {
        var variant = await _db.ProductVariants
            .Where(v => v.PartId == partId && v.Id == id)
            .Include(v => v.Attributes)
                .ThenInclude(av => av.Attribute)
            .Include(v => v.Attributes)
                .ThenInclude(av => av.Option)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (variant is null) return NotFound();
        return Ok(MapVariant(variant));
    }

    // POST /api/parts/{partId}/variants
    [HttpPost]
    public async Task<IActionResult> Create(Guid partId, [FromBody] CreateVariantRequest req, CancellationToken ct)
    {
        try
        {
            if (!await _db.Parts.AnyAsync(p => p.Id == partId, ct))
                return NotFound(new { message = "Product not found" });

            if (!string.IsNullOrWhiteSpace(req.SKU) &&
                await _db.ProductVariants.AnyAsync(v => v.SKU == req.SKU.Trim().ToUpperInvariant(), ct))
                return Conflict(new { message = $"Variant SKU '{req.SKU}' already exists" });

            if (await _db.ProductVariants.AnyAsync(v => v.PartId == partId && v.Code == req.Code.Trim().ToUpperInvariant(), ct))
                return Conflict(new { message = $"Variant code '{req.Code}' already exists for this product" });

            var variant = ProductVariant.Create(
                partId, req.Name, req.Code, req.SKU?.Trim(), req.Barcode?.Trim(),
                req.CostPrice, req.SellingPrice, req.Currency ?? "BDT", req.IsActive,
                req.WeightKg, req.WidthCm, req.HeightCm, req.DepthCm);

            var user = _currentUserService.GetCurrentUsername();
            variant.CreatedBy = user;
            variant.ModifiedBy = user;

            _db.ProductVariants.Add(variant);
            await _db.SaveChangesAsync(ct); // Save to get the variant Id

            // Auto-record price history when variant is created with a price
            if (req.SellingPrice > 0)
                await SyncVariantPriceHistory(partId, variant.Id, req.SellingPrice.Value, req.Currency ?? "BDT", user, ct);

            // Attach attribute values
            await SaveAttributeValues(variant.Id, req.AttributeValues, ct);

            // Reload with navigation properties
            await _db.Entry(variant).Collection(v => v.Attributes).Query()
                .Include(av => av.Attribute)
                .Include(av => av.Option)
                .LoadAsync(ct);

            return CreatedAtAction(nameof(GetById), new { partId, id = variant.Id }, MapVariant(variant));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating variant for part {PartId}", partId);
            return StatusCode(500, "An error occurred");
        }
    }

    // PUT /api/parts/{partId}/variants/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid partId, Guid id, [FromBody] CreateVariantRequest req, CancellationToken ct)
    {
        try
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Attributes)
                .FirstOrDefaultAsync(v => v.PartId == partId && v.Id == id, ct);

            if (variant is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.SKU) &&
                await _db.ProductVariants.AnyAsync(v => v.SKU == req.SKU.Trim().ToUpperInvariant() && v.Id != id, ct))
                return Conflict(new { message = $"Variant SKU '{req.SKU}' is used by another variant" });

            if (await _db.ProductVariants.AnyAsync(v => v.PartId == partId && v.Code == req.Code.Trim().ToUpperInvariant() && v.Id != id, ct))
                return Conflict(new { message = $"Variant code '{req.Code}' is used by another variant of this product" });

            var oldSellingPrice = variant.SellingPrice;
            var user = _currentUserService.GetCurrentUsername();

            variant.Update(req.Name, req.Code, req.SKU?.Trim(), req.Barcode?.Trim(),
                req.CostPrice, req.SellingPrice, req.Currency ?? "BDT", req.IsActive,
                req.WeightKg, req.WidthCm, req.HeightCm, req.DepthCm);
            variant.ModifiedBy = user;

            // Replace attribute values
            _db.VariantAttributeValues.RemoveRange(variant.Attributes);
            await _db.SaveChangesAsync(ct);

            // Auto-record price history when variant price changes
            if (req.SellingPrice > 0 && req.SellingPrice != oldSellingPrice)
                await SyncVariantPriceHistory(partId, variant.Id, req.SellingPrice.Value, req.Currency ?? "BDT", user, ct);

            await SaveAttributeValues(variant.Id, req.AttributeValues, ct);

            // Reload
            await _db.Entry(variant).Collection(v => v.Attributes).Query()
                .Include(av => av.Attribute)
                .Include(av => av.Option)
                .LoadAsync(ct);

            return Ok(MapVariant(variant));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating variant {VariantId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    // DELETE /api/parts/{partId}/variants/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid partId, Guid id, CancellationToken ct)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.PartId == partId && v.Id == id, ct);

        if (variant is null) return NotFound();

        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SyncVariantPriceHistory(Guid partId, Guid variantId, decimal sellingPrice, string currency, string user, CancellationToken ct)
    {
        if (sellingPrice <= 0) return;
        try
        {
            var record = ProductVariantPriceHistory.Create(partId, sellingPrice, DateTime.UtcNow, variantId, currency, "VARIANT_PRICE_SET");
            record.CreatedBy = user;
            record.ModifiedBy = user;
            await _priceHistoryRepository.SetNewPriceAsync(record, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create price history for variant {VariantId}", variantId);
        }
    }

    private async Task SaveAttributeValues(Guid variantId, List<VariantAttributeValueRequest>? values, CancellationToken ct)
    {
        if (values is null || values.Count == 0) return;

        var user = _currentUserService.GetCurrentUsername();
        foreach (var v in values)
        {
            var av = VariantAttributeValue.Create(
                variantId, v.AttributeId, v.OptionId,
                v.ValueText ?? "", v.ValueNumber, v.ValueBool);
            av.CreatedBy = user;
            av.ModifiedBy = user;
            _db.VariantAttributeValues.Add(av);
        }
        await _db.SaveChangesAsync(ct);
    }

    private static object MapVariant(ProductVariant v) => new
    {
        v.Id,
        v.PartId,
        v.Name,
        v.Code,
        v.SKU,
        v.Barcode,
        v.CostPrice,
        v.SellingPrice,
        v.Currency,
        v.IsActive,
        v.WeightKg,
        v.WidthCm,
        v.HeightCm,
        v.DepthCm,
        attributeValues = v.Attributes.Select(av => new
        {
            av.Id,
            av.AttributeId,
            attributeName  = av.Attribute?.Name,
            attributeCode  = av.Attribute?.Code,
            dataType       = av.Attribute?.DataType,
            av.OptionId,
            optionValue    = av.Option?.Value,
            av.ValueText,
            av.ValueNumber,
            av.ValueBool
        })
    };
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class CreateVariantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public string? Currency { get; set; } = "BDT";
    public bool IsActive { get; set; } = true;
    public decimal? WeightKg { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public List<VariantAttributeValueRequest> AttributeValues { get; set; } = new();
}

public class VariantAttributeValueRequest
{
    public Guid AttributeId { get; set; }
    public Guid? OptionId { get; set; }
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public bool? ValueBool { get; set; }
}
