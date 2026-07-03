using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.PartDtos;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Application.Parts;
using AppProductResponse = AutoPartShop.Application.Parts.Dtos.ProductResponse;
using AppProductPublicResponse = AutoPartShop.Application.Parts.Dtos.ProductPublicResponse;
using AppProductQuery = AutoPartShop.Application.Parts.Dtos.ProductQuery;
using SemanticSearchRequest = AutoPartShop.Application.Parts.Dtos.SemanticSearchRequest;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Services.Embedding;
using AutoPartsShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Products API — v1.
/// All list/search is handled by GET / with query parameters.
/// Authenticated callers receive CostPrice in pricing objects; anonymous callers do not.
/// Variants always contains at least one entry; products with no explicit variants receive a
/// synthesized "Default" variant built from the base product.
/// </summary>
[Route("api/v1/products")]
[ApiController]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IProductReadRepository _productReadRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPartVehicleCompatibilityRepository _compatibilityRepository;
    private readonly IProductVariantPriceHistoryRepository _variantPriceHistoryRepository;
    private readonly IWarrantyRegistrationRepository _warrantyRegistrationRepository;
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IProductEmbeddingRepository _embeddingRepository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductRepository productRepository,
        IProductReadRepository productReadRepository,
        ICategoryRepository categoryRepository,
        IPartVehicleCompatibilityRepository compatibilityRepository,
        IProductVariantPriceHistoryRepository variantPriceHistoryRepository,
        IWarrantyRegistrationRepository warrantyRegistrationRepository,
        IStockLevelRepository stockLevelRepository,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        IEmbeddingService embeddingService,
        IProductEmbeddingRepository embeddingRepository,
        ILogger<ProductsController> logger)
    {
        _productRepository = productRepository;
        _productReadRepository = productReadRepository;
        _categoryRepository = categoryRepository;
        _compatibilityRepository = compatibilityRepository;
        _variantPriceHistoryRepository = variantPriceHistoryRepository;
        _warrantyRegistrationRepository = warrantyRegistrationRepository;
        _stockLevelRepository = stockLevelRepository;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
        _logger = logger;
    }

    // ── List ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// List products with optional filtering and pagination.
    /// Omit isActive to return all; set isActive=true for active only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool flattenVariants = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        else if (pageSize > 100) pageSize = 100;

        var query = new AppProductQuery
        {
            Search = search ?? string.Empty,
            PageNumber = page,
            PageSize = pageSize,
            IsActive = isActive,
            FlattenVariants = flattenVariants,
            CategoryId = categoryId
        };

        var isAdmin = User.Identity?.IsAuthenticated == true;

        if (isAdmin)
        {
            var (items, total) = await _productReadRepository.FindAllAsync(query, cancellationToken);
            return Ok(PagedApiResponse<AppProductResponse>.Create(items, total, page, pageSize));
        }
        else
        {
            var (items, total) = await _productReadRepository.FindAllPublicAsync(query, cancellationToken);
            return Ok(PagedApiResponse<AppProductPublicResponse>.Create(items, total, page, pageSize));
        }
    }

    // ── Single ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Get a product by ID with fully nested variants array.
    /// Always returns at least one variant (synthesized "Default" if none exist).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var part = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (part is null)
            return NotFound(ApiError.NotFound($"Product '{id}' not found", Request.Path));

        var isAdmin = User.Identity?.IsAuthenticated == true;
        return Ok(ApiResponse<ProductResponse>.Ok(MapToProductResponse(part, isAdmin)));
    }

    // ── Code lookup ───────────────────────────────────────────────────────────

    /// <summary>
    /// Look up a product by SKU, barcode, or part number.
    /// Returns the current FIFO lot selling price and available stock.
    /// Used for barcode scanning and POS price-check dialogs.
    /// </summary>
    [HttpGet("by-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode([FromQuery] string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(ApiError.Validation("'code' query parameter is required", instance: Request.Path));

        var normalizedCode = code.Trim().ToUpperInvariant();

        // 1. Try product-level SKU / barcode / part number
        var part = await _productRepository.GetBySKUAsync(normalizedCode, cancellationToken)
                   ?? await _productRepository.GetByBarcodeOrPartNumberAsync(normalizedCode, cancellationToken);

        if (part is not null)
        {
            var totalStock  = await GetTotalAvailableStockAsync(part.Id, null, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new
            {
                productId            = part.Id,
                name                 = part.Name,
                sku                  = part.SKU,
                partNumber           = part.PartNumber?.Value ?? string.Empty,
                sellingPrice         = part.SellingPrice,
                fallbackSellingPrice = part.SellingPrice,
                hasLotPrice          = false,
                stockLevel           = totalStock,
                unitId               = part.UnitId,
                unitName             = part.Unit?.Name,
                variantId            = (Guid?)null,
                variantName          = (string?)null,
                variantCode          = (string?)null
            }));
        }

        // 2. Try variant-level SKU / barcode
        var variantMatch = await _productRepository.GetByVariantCodeAsync(normalizedCode, cancellationToken);
        if (variantMatch is null)
            return NotFound(ApiError.NotFound($"No product found for code '{code}'", Request.Path));

        var (variantPart, variant) = variantMatch.Value;
        var variantTotalStock = await GetTotalAvailableStockAsync(variantPart.Id, variant.Id, cancellationToken);
        var variantPrice      = CatalogPrice.Resolve(variantPart.SellingPrice, variant.SellingPrice);

        return Ok(ApiResponse<object>.Ok(new
        {
            productId            = variantPart.Id,
            name                 = variantPart.Name,
            sku                  = variantPart.SKU,
            partNumber           = variantPart.PartNumber?.Value ?? string.Empty,
            sellingPrice         = variantPrice,
            fallbackSellingPrice = variantPrice,
            hasLotPrice          = false,
            stockLevel           = variantTotalStock,
            unitId               = variantPart.UnitId,
            unitName             = variantPart.Unit?.Name,
            variantId            = variant.Id,
            variantName          = variant.Name,
            variantCode          = variant.Code
        }));
    }

    // ── Lot price ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Get the current FIFO lot selling price and stock availability for a product.
    /// </summary>
    [HttpGet("{id:guid}/lot-price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLotPrice(Guid id, [FromQuery] Guid? variantId, CancellationToken cancellationToken)
    {
        var part = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (part is null)
            return NotFound(ApiError.NotFound($"Product '{id}' not found", Request.Path));

        var totalStock = await GetTotalAvailableStockAsync(id, variantId, cancellationToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            productId = id,
            sellingPrice = part.SellingPrice,
            lotSellingPrice = (decimal?)null,
            fallbackSellingPrice = part.SellingPrice,
            hasLotPrice = false,
            stockAvailable = totalStock
        }));
    }

    // ── Compatible vehicles ───────────────────────────────────────────────────

    /// <summary>Get all vehicles compatible with this product.</summary>
    [HttpGet("{id:guid}/compatible-vehicles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompatibleVehicles(Guid id, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(id, cancellationToken))
            return NotFound(ApiError.NotFound($"Product '{id}' not found", Request.Path));

        var compatibilities = await _compatibilityRepository.GetCompatibilitiesByPartAsync(id, cancellationToken);
        var response = compatibilities.Select(c => new
        {
            id = c.Id,
            vehicleId = c.VehicleId,
            vehicleMake = c.Vehicle?.Make ?? string.Empty,
            vehicleModel = c.Vehicle?.Model ?? string.Empty,
            vehicleYear = c.Vehicle?.Year ?? 0,
            vehicleEngineType = c.Vehicle?.EngineType ?? string.Empty,
            vehicleInfo = c.Vehicle != null ? $"{c.Vehicle.Make} {c.Vehicle.Model} {c.Vehicle.Year}" : string.Empty,
            isCompatible = c.IsCompatible,
            notes = c.Notes
        });

        return Ok(ApiResponse<object>.Ok(response));
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePartRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.PartNumber) || request.CategoryId == Guid.Empty)
            return BadRequest(ApiError.Validation("Name, PartNumber, and CategoryId are required", instance: Request.Path));

        if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
            return BadRequest(ApiError.Validation("Category does not exist", instance: Request.Path));

        var sku = await _codeGenerateService.GenerateAsync("SKU", cancellationToken);
        var partNumber = PartNumber.Create(request.PartNumber);
        var part = Product.Create(
            request.Name, partNumber, sku, request.CategoryId,
            request.BrandId, request.BaseUnitId, request.UnitId,
            request.Description, request.RichDescription,
            request.CostPrice, request.SellingPrice, request.MinimumStock,
            request.HasWarranty, request.WarrantyPeriodMonths, request.WarrantyType,
            request.WarrantyTerms, request.WarrantyCertificateTemplate,
            request.Barcode, request.Tags, request.ProductType, request.IsPerishable,
            request.WeightKg, request.WidthCm, request.HeightCm, request.DepthCm, request.TaxCode,
            request.OemNumber, request.LocalName);

        var currentUser = _currentUserService.GetCurrentUsername();
        part.CreatedBy = currentUser;
        part.ModifiedBy = currentUser;

        await _productRepository.AddAsync(part, cancellationToken);

        if (request.SellingPrice > 0)
        {
            var initialPrice = ProductVariantPriceHistory.Create(
                part.Id, request.SellingPrice, DateTime.UtcNow, null,
                part.SellingPriceCurrency, "INITIAL_PRICE");
            initialPrice.CreatedBy = currentUser;
            initialPrice.ModifiedBy = currentUser;
            await _variantPriceHistoryRepository.AddAsync(initialPrice, cancellationToken);
        }

        // Re-fetch so Category/Brand names are available for richer embedding text.
        var withNavs = await _productRepository.GetByIdAsync(part.Id, cancellationToken) ?? part;
        await TryUpsertEmbeddingAsync(withNavs, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = part.Id },
            ApiResponse<ProductResponse>.Ok(MapToProductResponse(part, isAdmin: true)));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.CategoryId == Guid.Empty)
            return BadRequest(ApiError.Validation("Name and CategoryId are required", instance: Request.Path));

        var part = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (part is null)
            return NotFound(ApiError.NotFound($"Product '{id}' not found", Request.Path));

        if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
            return BadRequest(ApiError.Validation("Category does not exist", instance: Request.Path));

        var oldSellingPrice = part.SellingPrice;
        var oldWarranty = (part.HasWarranty, part.WarrantyPeriodMonths, part.WarrantyType,
                           part.WarrantyTerms, part.WarrantyCertificateTemplate);

        part.Update(request.Name, request.Description, part.SKU, request.CategoryId, request.BrandId,
            request.BaseUnitId, request.UnitId,
            request.CostPrice, request.SellingPrice, request.MinimumStock, request.IsActive,
            request.HasWarranty, request.WarrantyPeriodMonths, request.WarrantyType,
            request.WarrantyTerms, request.WarrantyCertificateTemplate,
            request.Barcode, request.Tags, request.ProductType,
            request.IsPerishable, request.WeightKg, request.WidthCm, request.HeightCm, request.DepthCm,
            request.TaxCode, request.RichDescription, request.OemNumber, request.LocalName);
        part.ModifiedBy = _currentUserService.GetCurrentUsername();

        await _productRepository.UpdateAsync(part, cancellationToken);

        var warrantyChanged =
            oldWarranty.HasWarranty != part.HasWarranty ||
            oldWarranty.WarrantyPeriodMonths != part.WarrantyPeriodMonths ||
            !string.Equals(oldWarranty.WarrantyType, part.WarrantyType, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(oldWarranty.WarrantyTerms, part.WarrantyTerms, StringComparison.Ordinal) ||
            !string.Equals(oldWarranty.WarrantyCertificateTemplate, part.WarrantyCertificateTemplate, StringComparison.Ordinal);

        if (warrantyChanged)
            await SyncWarrantyRegistrationsAsync(part, cancellationToken);

        // Sync price schedule when selling price changes (AuditLog captures the field change automatically)
        if (oldSellingPrice != request.SellingPrice && request.SellingPrice > 0)
        {
            var user = _currentUserService.GetCurrentUsername();
            var vph = ProductVariantPriceHistory.Create(
                part.Id, request.SellingPrice, DateTime.UtcNow, null,
                part.SellingPriceCurrency, "PRICE_UPDATE");
            vph.CreatedBy = user;
            vph.ModifiedBy = user;
            await _variantPriceHistoryRepository.SetNewPriceAsync(vph, cancellationToken);
        }

        // Keep the embedding in sync with the latest product fields (part has Category/Brand loaded).
        await TryUpsertEmbeddingAsync(part, cancellationToken);

        return Ok(ApiResponse<ProductResponse>.Ok(MapToProductResponse(part, isAdmin: true)));
    }

    // ── Semantic search ─────────────────────────────────────────────────────────

    /// <summary>
    /// Natural-language product search. Embeds the query and ranks products by cosine similarity
    /// (SQL Server vector search). Falls back to keyword search when embeddings are not configured.
    /// </summary>
    [HttpPost("search-semantic")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSemantic([FromBody] SemanticSearchRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(ApiError.Validation("Query is required", instance: Request.Path));

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var vector = await _embeddingService.EmbedAsync(request.Query, cancellationToken);

        // Graceful fallback: embeddings unconfigured/unavailable → keyword search, same response shape.
        if (vector is null)
        {
            var query = new AppProductQuery
            {
                Search = request.Query,
                PageNumber = page,
                PageSize = pageSize,
                IsActive = request.IsActive,
                FlattenVariants = false
            };
            var (kw, kwTotal) = await _productReadRepository.FindAllAsync(query, cancellationToken);
            return Ok(PagedApiResponse<AppProductResponse>.Create(kw, kwTotal, page, pageSize));
        }

        var (items, total) = await _productReadRepository.SearchSemanticAsync(
            vector, request.IsActive, page, pageSize, cancellationToken);
        return Ok(PagedApiResponse<AppProductResponse>.Create(items, total, page, pageSize));
    }

    /// <summary>
    /// Generate (or refresh) the product's embedding and store it. Best-effort: any failure is logged
    /// and swallowed so it never blocks the product create/update that already committed.
    /// </summary>
    private async Task TryUpsertEmbeddingAsync(Product part, CancellationToken cancellationToken)
    {
        if (!_embeddingService.IsEnabled) return;

        try
        {
            var text = EmbeddingTextBuilder.Build(part);
            if (string.IsNullOrWhiteSpace(text)) return;

            var vector = await _embeddingService.EmbedAsync(text, cancellationToken);
            if (vector is null) return;

            var partNumber = part.PartNumber?.Value ?? string.Empty;
            var existing = await _embeddingRepository.GetByProductIdAsync(part.Id, cancellationToken);
            if (existing is null)
            {
                var embedding = ProductEmbedding.Create(part.Id, vector, _embeddingService.Model, text, partNumber, part.OemNumber);
                embedding.CreatedBy = _currentUserService.GetCurrentUsername();
                embedding.ModifiedBy = embedding.CreatedBy;
                await _embeddingRepository.UpsertAsync(embedding, cancellationToken);
            }
            else
            {
                existing.Update(vector, _embeddingService.Model, text, partNumber, part.OemNumber);
                existing.ModifiedBy = _currentUserService.GetCurrentUsername();
                await _embeddingRepository.UpsertAsync(existing, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert embedding for product {ProductId}", part.Id);
        }
    }

    // ── Status ────────────────────────────────────────────────────────────────

    /// <summary>Activate or deactivate a product. Body: { "isActive": true|false }</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetProductStatusRequest request, CancellationToken cancellationToken)
    {
        var part = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (part is null)
            return NotFound(ApiError.NotFound($"Product '{id}' not found", Request.Path));

        if (request.IsActive) part.Activate(); else part.Deactivate();
        part.ModifiedBy = _currentUserService.GetCurrentUsername();
        await _productRepository.UpdateAsync(part, cancellationToken);

        return Ok(ApiResponse<ProductResponse>.Ok(MapToProductResponse(part, isAdmin: true)));
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(id, cancellationToken))
            return NotFound(ApiError.NotFound($"Product '{id}' not found", Request.Path));

        await _productRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static ProductResponse MapToProductResponse(Product part, bool isAdmin)
    {
        var hasDimensions = part.WeightKg.HasValue || part.WidthCm.HasValue || part.HeightCm.HasValue || part.DepthCm.HasValue;
        var hasWarrantyData = part.HasWarranty || part.WarrantyPeriodMonths.HasValue;

        var explicitVariants = part.Variants?
            .Where(v => !v.Isdeleted)
            .OrderBy(v => v.Name)
            .Select(v => MapVariantSummary(v, isAdmin))
            .ToList() ?? [];

        // Synthesize a default variant when the product has none
        var variants = explicitVariants.Count > 0
            ? explicitVariants
            : [SynthesizeDefaultVariant(part, isAdmin)];

        return new ProductResponse
        {
            Id = part.Id,
            Name = part.Name,
            Description = part.Description,
            RichDescription = part.RichDescription,
            PartNumber = part.PartNumber.Value,
            SKU = part.SKU,
            OemNumber = part.OemNumber,
            LocalName = part.LocalName,
            VehicleFit = BuildVehicleFitSummary(part.VehicleCompatibilities),
            Barcode = part.Barcode,
            Tags = part.Tags,
            ProductType = part.ProductType,
            IsPerishable = part.IsPerishable,
            IsActive = part.IsActive,
            MinimumStock = part.MinimumStock,
            TaxCode = part.TaxCode,
            HasVariants = explicitVariants.Count > 0,
            Category = new ProductCategorySummary
            {
                Id = part.CategoryId,
                Name = part.Category?.Name ?? string.Empty,
                Breadcrumb = part.Category?.BreadcrumbPath
            },
            Brand = part.BrandId.HasValue ? new ProductBrandSummary
            {
                Id = part.BrandId.Value,
                Name = part.Brand?.Name ?? string.Empty
            } : null,
            BaseUnit = part.BaseUnitId.HasValue ? new ProductUnitSummary
            {
                Id = part.BaseUnitId.Value,
                Name = part.BaseUnit?.Name ?? string.Empty
            } : null,
            Unit = part.UnitId.HasValue ? new ProductUnitSummary
            {
                Id = part.UnitId.Value,
                Name = part.Unit?.Name ?? string.Empty
            } : null,
            Pricing = new ProductPricingSummary
            {
                CostPrice = isAdmin ? part.CostPrice : null,
                SellingPrice = part.SellingPrice,
                Currency = part.SellingPriceCurrency ?? "BDT"
            },
            Dimensions = hasDimensions ? new ProductDimensionsSummary
            {
                WeightKg = part.WeightKg,
                WidthCm = part.WidthCm,
                HeightCm = part.HeightCm,
                DepthCm = part.DepthCm
            } : null,
            Warranty = hasWarrantyData ? new ProductWarrantySummary
            {
                HasWarranty = part.HasWarranty,
                PeriodMonths = part.WarrantyPeriodMonths,
                Type = part.WarrantyType,
                Terms = part.WarrantyTerms,
                CertificateTemplate = part.WarrantyCertificateTemplate
            } : null,
            Variants = variants,
            CreatedBy = isAdmin ? part.CreatedBy : null,
            ModifiedBy = isAdmin ? part.ModifiedBy : null,
            CreatedAt = part.CreatedDate,
            UpdatedAt = part.ModifiedDate
        };
    }

    private static ProductVariantSummary SynthesizeDefaultVariant(Product part, bool isAdmin) => new()
    {
        Id = null,
        Name = "Default",
        Code = "DEFAULT",
        SKU = part.SKU,
        Barcode = part.Barcode,
        IsDefault = true,
        IsActive = part.IsActive,
        Pricing = new ProductPricingSummary
        {
            CostPrice = isAdmin ? part.CostPrice : null,
            SellingPrice = part.SellingPrice,
            Currency = part.SellingPriceCurrency ?? "BDT"
        },
        Attributes = []
    };

    private static ProductVariantSummary MapVariantSummary(ProductVariant v, bool isAdmin) => new()
    {
        Id = v.Id,
        Name = v.Name,
        Code = v.Code,
        SKU = v.SKU,
        Barcode = v.Barcode,
        IsDefault = false,
        IsActive = v.IsActive,
        Pricing = new ProductPricingSummary
        {
            CostPrice = isAdmin ? v.CostPrice : null,
            SellingPrice = v.SellingPrice,
            Currency = v.Currency ?? "BDT"
        },
        Attributes = v.Attributes?.Select(av => new VariantAttributeSummary
        {
            AttributeId = av.AttributeId,
            AttributeName = av.Attribute?.Name ?? string.Empty,
            DataType = av.Attribute?.DataType,
            OptionId = av.OptionId,
            OptionValue = av.Option?.Value,
            ValueText = av.ValueText,
            ValueNumber = av.ValueNumber,
            ValueBool = av.ValueBool
        }).ToList() ?? []
    };

    private static string? BuildVehicleFitSummary(ICollection<PartVehicleCompatibility> compatibilities)
    {
        var vehicles = compatibilities
            .Where(vc => !vc.Isdeleted && vc.IsCompatible && vc.Vehicle != null)
            .OrderBy(vc => vc.Vehicle!.Make)
            .ToList();

        if (vehicles.Count == 0) return null;

        var labels = vehicles.Take(2).Select(vc => $"{vc.Vehicle!.Make} {vc.Vehicle.Model} {vc.Vehicle.Year}");
        var summary = string.Join(", ", labels);
        if (vehicles.Count > 2)
            summary += $" +{vehicles.Count - 2}";
        return summary;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Sellable stock for a part = on-hand minus reservations (e.g. open ecommerce carts),
    /// read from StockLevel which is the source of truth for availability. When
    /// <paramref name="variantId"/> is supplied only that variant is counted, so two variants of
    /// the same part never report each other's stock; otherwise all variants are summed.
    /// </summary>
    private async Task<int> GetTotalAvailableStockAsync(Guid partId, Guid? variantId, CancellationToken ct)
    {
        var levels = variantId.HasValue
            ? await _stockLevelRepository.GetByPartAndVariantAsync(partId, variantId, ct)
            : await _stockLevelRepository.GetByPartAsync(partId, ct);

        return levels.Sum(sl =>
        {
            var onHand   = sl.QuantityOnHandInBaseUnit   > 0 ? sl.QuantityOnHandInBaseUnit   : sl.QuantityOnHand;
            var reserved = sl.QuantityReservedInBaseUnit > 0 ? sl.QuantityReservedInBaseUnit : sl.QuantityReserved;
            var available = onHand - reserved;
            return available > 0 ? available : 0;
        });
    }

    private async Task SyncWarrantyRegistrationsAsync(Product part, CancellationToken ct)
    {
        var warranties = (await _warrantyRegistrationRepository.GetByPartIdAsync(part.Id, ct)).ToList();
        if (warranties.Count == 0) return;
        var user = _currentUserService.GetCurrentUsername();
        foreach (var w in warranties)
        {
            w.SyncFromPartWarranty(part.HasWarranty, part.WarrantyPeriodMonths, part.WarrantyType,
                part.WarrantyTerms, part.WarrantyCertificateTemplate, user, "Part warranty updated");
            await _warrantyRegistrationRepository.UpdateAsync(w, ct);
        }
        _logger.LogInformation("Synced {Count} warranty registrations for product {Id}", warranties.Count, part.Id);
    }
}

public class SetProductStatusRequest
{
    public bool IsActive { get; set; }
}
