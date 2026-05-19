using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/v1/products/{productId:guid}/variants")]
[ApiController]
[Authorize]
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

    // GET /api/v1/products/{productId}/variants
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid productId, CancellationToken ct)
    {
        if (!await _db.Parts.AnyAsync(p => p.Id == productId, ct))
            return NotFound(ApiError.NotFound($"Product '{productId}' not found", Request.Path));

        var variants = await _db.ProductVariants
            .Where(v => v.PartId == productId)
            .Include(v => v.Attributes).ThenInclude(av => av.Attribute)
            .Include(v => v.Attributes).ThenInclude(av => av.Option)
            .OrderBy(v => v.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(variants.Select(MapVariant)));
    }

    // GET /api/v1/products/{productId}/variants/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid productId, Guid id, CancellationToken ct)
    {
        var variant = await _db.ProductVariants
            .Where(v => v.PartId == productId && v.Id == id)
            .Include(v => v.Attributes).ThenInclude(av => av.Attribute)
            .Include(v => v.Attributes).ThenInclude(av => av.Option)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (variant is null)
            return NotFound(ApiError.NotFound($"Variant '{id}' not found on product '{productId}'", Request.Path));

        return Ok(ApiResponse<object>.Ok(MapVariant(variant)));
    }

    // POST /api/v1/products/{productId}/variants
    [HttpPost]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateVariantRequest req, CancellationToken ct)
    {
        if (!await _db.Parts.AnyAsync(p => p.Id == productId, ct))
            return NotFound(ApiError.NotFound($"Product '{productId}' not found", Request.Path));

        if (!string.IsNullOrWhiteSpace(req.SKU) &&
            await _db.ProductVariants.AnyAsync(v => v.SKU == req.SKU.Trim().ToUpperInvariant(), ct))
            return Conflict(ApiError.Conflict($"Variant SKU '{req.SKU}' already exists", Request.Path));

        if (await _db.ProductVariants.AnyAsync(v => v.PartId == productId && v.Code == req.Code.Trim().ToUpperInvariant(), ct))
            return Conflict(ApiError.Conflict($"Variant code '{req.Code}' already exists for this product", Request.Path));

        var variant = ProductVariant.Create(
            productId, req.Name, req.Code,
            req.CostPrice, req.SellingPrice,
            req.SKU?.Trim(), req.Barcode?.Trim(),
            req.Currency ?? "BDT", req.IsActive,
            req.WeightKg, req.WidthCm, req.HeightCm, req.DepthCm);

        var user = _currentUserService.GetCurrentUsername();
        variant.CreatedBy = user;
        variant.ModifiedBy = user;

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync(ct);

        if (req.SellingPrice > 0)
            await SyncVariantPriceHistory(productId, variant.Id, req.SellingPrice, req.Currency ?? "BDT", user, ct);

        await SaveAttributeValues(variant.Id, req.AttributeValues, ct);

        await _db.Entry(variant).Collection(v => v.Attributes).Query()
            .Include(av => av.Attribute)
            .Include(av => av.Option)
            .LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { productId, id = variant.Id },
            ApiResponse<object>.Ok(MapVariant(variant)));
    }

    // PUT /api/v1/products/{productId}/variants/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] CreateVariantRequest req, CancellationToken ct)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Attributes)
            .FirstOrDefaultAsync(v => v.PartId == productId && v.Id == id, ct);

        if (variant is null)
            return NotFound(ApiError.NotFound($"Variant '{id}' not found on product '{productId}'", Request.Path));

        if (!string.IsNullOrWhiteSpace(req.SKU) &&
            await _db.ProductVariants.AnyAsync(v => v.SKU == req.SKU.Trim().ToUpperInvariant() && v.Id != id, ct))
            return Conflict(ApiError.Conflict($"Variant SKU '{req.SKU}' is used by another variant", Request.Path));

        if (await _db.ProductVariants.AnyAsync(v => v.PartId == productId && v.Code == req.Code.Trim().ToUpperInvariant() && v.Id != id, ct))
            return Conflict(ApiError.Conflict($"Variant code '{req.Code}' is used by another variant of this product", Request.Path));

        var oldSellingPrice = variant.SellingPrice;
        var user = _currentUserService.GetCurrentUsername();

        variant.Update(req.Name, req.Code,
            req.CostPrice, req.SellingPrice,
            req.SKU?.Trim(), req.Barcode?.Trim(),
            req.Currency ?? "BDT", req.IsActive,
            req.WeightKg, req.WidthCm, req.HeightCm, req.DepthCm);
        variant.ModifiedBy = user;

        _db.VariantAttributeValues.RemoveRange(variant.Attributes);
        await _db.SaveChangesAsync(ct);

        if (req.SellingPrice > 0 && req.SellingPrice != oldSellingPrice)
            await SyncVariantPriceHistory(productId, variant.Id, req.SellingPrice, req.Currency ?? "BDT", user, ct);

        await SaveAttributeValues(variant.Id, req.AttributeValues, ct);

        await _db.Entry(variant).Collection(v => v.Attributes).Query()
            .Include(av => av.Attribute)
            .Include(av => av.Option)
            .LoadAsync(ct);

        return Ok(ApiResponse<object>.Ok(MapVariant(variant)));
    }

    // DELETE /api/v1/products/{productId}/variants/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid productId, Guid id, CancellationToken ct)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.PartId == productId && v.Id == id, ct);

        if (variant is null)
            return NotFound(ApiError.NotFound($"Variant '{id}' not found on product '{productId}'", Request.Path));

        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SyncVariantPriceHistory(Guid productId, Guid variantId, decimal sellingPrice, string currency, string user, CancellationToken ct)
    {
        if (sellingPrice <= 0) return;
        try
        {
            var record = ProductVariantPriceHistory.Create(productId, sellingPrice, DateTime.UtcNow, variantId, currency, "VARIANT_PRICE_SET");
            record.CreatedBy = user;
            record.ModifiedBy = user;
            await _priceHistoryRepository.SetNewPriceAsync(record, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync price schedule for variant {VariantId}", variantId);
        }
    }

    private async Task SaveAttributeValues(Guid variantId, List<VariantAttributeValueRequest>? values, CancellationToken ct)
    {
        if (values is null || values.Count == 0) return;
        var user = _currentUserService.GetCurrentUsername();
        foreach (var v in values)
        {
            var av = VariantAttributeValue.Create(variantId, v.AttributeId, v.OptionId,
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
        productId = v.PartId,
        v.Name,
        v.Code,
        v.SKU,
        v.Barcode,
        v.PricingMode,
        v.CostPrice,
        v.SellingPrice,
        v.Currency,
        v.IsActive,
        v.WeightKg,
        v.WidthCm,
        v.HeightCm,
        v.DepthCm,
        attributes = v.Attributes.Select(av => new
        {
            av.Id,
            av.AttributeId,
            attributeName = av.Attribute?.Name,
            attributeCode = av.Attribute?.Code,
            dataType      = av.Attribute?.DataType,
            av.OptionId,
            optionValue   = av.Option?.Value,
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
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
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
