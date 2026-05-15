using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.PartDtos;
using AutoPartShop.Application.Parts;
using AutoPartShop.Application.Parts.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartsShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;


namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class PartsController : ControllerBase
{
    private readonly IPartRepository _partRepository;
    private readonly IPartReadRepository _partReadRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly IPartVehicleCompatibilityRepository _compatibilityRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IProductVariantPriceHistoryRepository _variantPriceHistoryRepository;
    private readonly IWarrantyRegistrationRepository _warrantyRegistrationRepository;
    private readonly IStockLotRepository _stockLotRepository;
    private readonly ILogger<PartsController> _logger;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;

    public PartsController(
        IPartRepository partRepository,
        IPartReadRepository partReadRepository,
        ICategoryRepository categoryRepository,
        IUnitRepository unitRepository,
        IPartVehicleCompatibilityRepository compatibilityRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IProductVariantPriceHistoryRepository variantPriceHistoryRepository,
        IWarrantyRegistrationRepository warrantyRegistrationRepository,
        IStockLotRepository stockLotRepository,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ILogger<PartsController> logger)
    {
        _partRepository = partRepository;
        _partReadRepository = partReadRepository;
        _categoryRepository = categoryRepository;
        _unitRepository = unitRepository;
        _compatibilityRepository = compatibilityRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _variantPriceHistoryRepository = variantPriceHistoryRepository;
        _warrantyRegistrationRepository = warrantyRegistrationRepository;
        _stockLotRepository = stockLotRepository;
        _logger = logger;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
    }


    [HttpPost("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FindAll([FromBody] PartQuery query, CancellationToken cancellationToken)
    {
        try
        {
            if (query is null)
            {
                return BadRequest("Request can not be empty");
            }
            if (query.PageNumber < 0)
            {
                return BadRequest($"Page number can not be {query.PageNumber}");
            }
            if (query.PageSize < 0)
            {
                return BadRequest($"Page size can not be {query.PageSize}");
            }

            var (response, total) = await _partReadRepository.FindAllAsync(query, cancellationToken);
            return Ok(PagedResult<PartResponse>.Create(response, total, query));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customers");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customers");
        }
    }

    [HttpPost("public/list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FindAllPublic([FromBody] PartQuery query, CancellationToken cancellationToken)
    {
        try
        {
            if (query is null)
            {
                return BadRequest("Request can not be empty");
            }
            if (query.PageNumber < 0)
            {
                return BadRequest($"Page number can not be {query.PageNumber}");
            }
            if (query.PageSize < 0)
            {
                return BadRequest($"Page size can not be {query.PageSize}");
            }

            var (response, total) = await _partReadRepository.FindAllPublicAsync(query, cancellationToken);
            return Ok(PagedResult<PartPublicResponse>.Create(response, total, query));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public parts list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving parts");
        }
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PartResponse>))]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all parts");
            var parts = await _partRepository.GetAllAsync(cancellationToken);
            var response = parts.Select(p => MapToResponse(p));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all parts");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving parts");
        }
    }

    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PartResponse>))]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all active parts");
            var parts = await _partRepository.GetAllActiveAsync(cancellationToken);
            var response = parts.Select(p => MapToResponse(p));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active parts");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active parts");
        }
    }




    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PartResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting part by ID: {PartId}", id);
            var part = await _partRepository.GetByIdAsync(id, cancellationToken);

            if (part is null)
            {
                _logger.LogWarning("Part not found: {PartId}", id);
                return NotFound(new { message = "Part not found" });
            }

            var response = MapToResponse(part);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting part by ID: {PartId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the part");
        }
    }

    [HttpGet("public/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PartPublicResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting public part by ID: {PartId}", id);
            var part = await _partRepository.GetByIdAsync(id, cancellationToken);

            if (part is null)
            {
                _logger.LogWarning("Public part not found: {PartId}", id);
                return NotFound(new { message = "Part not found" });
            }

            var response = MapToPublicResponse(part);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public part by ID: {PartId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the part");
        }
    }

    [HttpGet("public/active")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PartPublicResponse>))]
    public async Task<IActionResult> GetPublicActive(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all public active parts");
            var parts = await _partRepository.GetAllActiveAsync(cancellationToken);
            var response = parts.Select(p => MapToPublicResponse(p));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public active parts");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active parts");
        }
    }

    /// <summary>
    /// Search a part by SKU, barcode, or part number and return its current FIFO lot selling price.
    /// Falls back to Part.SellingPrice when no active lot has a price set.
    /// Used by barcode scanning and price-check dialogs in the POS.
    /// </summary>
    [HttpGet("search-by-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SearchByCode([FromQuery] string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "code is required" });

        try
        {
            var normalizedCode = code.Trim().ToUpperInvariant();
            var part = await _partRepository.GetBySKUAsync(normalizedCode, cancellationToken)
                       ?? (await _partRepository.GetAllActiveAsync(cancellationToken))
                           .FirstOrDefault(p =>
                               (p.Barcode != null && p.Barcode.ToUpperInvariant() == normalizedCode) ||
                               (p.PartNumber != null && p.PartNumber.Value.ToUpperInvariant() == normalizedCode));

            if (part is null)
                return NotFound(new { message = $"No part found for code '{code}'" });

            var lotPrice = await GetFifoLotSellingPriceAsync(part.Id, cancellationToken);
            var totalStock = await GetTotalAvailableStockAsync(part.Id, cancellationToken);

            return Ok(new
            {
                partId = part.Id,
                name = part.Name,
                sku = part.SKU,
                partNumber = part.PartNumber?.Value ?? string.Empty,
                sellingPrice = lotPrice > 0 ? lotPrice : part.SellingPrice,
                fallbackSellingPrice = part.SellingPrice,
                hasLotPrice = lotPrice > 0,
                stockLevel = totalStock,
                unitId = part.UnitId,
                unitName = part.Unit?.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching part by code: {Code}", code);
            return StatusCode(500, "An error occurred while searching for the part");
        }
    }

    /// <summary>
    /// Get the current FIFO lot selling price for a part (oldest active lot with price set).
    /// Returns the lot price and the fallback Part.SellingPrice.
    /// </summary>
    [HttpGet("public/{id:guid}/lot-price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLotPrice(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var part = await _partRepository.GetByIdAsync(id, cancellationToken);
            if (part is null)
                return NotFound(new { message = "Part not found" });

            var lotPrice = await GetFifoLotSellingPriceAsync(id, cancellationToken);
            var totalStock = await GetTotalAvailableStockAsync(id, cancellationToken);

            return Ok(new
            {
                partId = id,
                sellingPrice = lotPrice > 0 ? lotPrice : part.SellingPrice,
                lotSellingPrice = lotPrice > 0 ? (decimal?)lotPrice : null,
                fallbackSellingPrice = part.SellingPrice,
                hasLotPrice = lotPrice > 0,
                stockAvailable = totalStock
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lot price for part: {PartId}", id);
            return StatusCode(500, "An error occurred while retrieving the lot price");
        }
    }

    // Returns the selling price from the oldest active FIFO stock lot (0 if none set)
    private async Task<decimal> GetFifoLotSellingPriceAsync(Guid partId, CancellationToken cancellationToken)
    {
        var lots = await _stockLotRepository.GetByPartAsync(partId, cancellationToken);
        var activeLot = lots
            .Where(l => l.QuantityAvailable > 0 && !l.IsExpired)
            .OrderBy(l => l.ReceivingDate)
            .FirstOrDefault(l => l.SellingPrice > 0);
        return activeLot?.SellingPrice ?? 0m;
    }

    // Returns total available quantity across all warehouses for a part
    private async Task<int> GetTotalAvailableStockAsync(Guid partId, CancellationToken cancellationToken)
    {
        var lots = await _stockLotRepository.GetByPartAsync(partId, cancellationToken);
        return lots.Where(l => !l.IsExpired).Sum(l => l.QuantityAvailable);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PartResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreatePartRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.SKU) ||
                string.IsNullOrWhiteSpace(request.PartNumber) || request.CategoryId == Guid.Empty)
                return BadRequest(new { message = "Name, PartNumber, SKU, and CategoryId are required" });

            _logger.LogInformation("Creating new part: {PartName} ({SKU})", request.Name, request.SKU);

            // Verify category exists
            if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
                return BadRequest(new { message = "Category does not exist" });

            // Check for duplicate SKU
            if (await _partRepository.SKUExistsAsync(request.SKU, null, cancellationToken))
                return Conflict(new { message = $"Part SKU '{request.SKU}' already exists" });

            var partNumber = PartNumber.Create(request.PartNumber);

            var part = Part.Create(
                request.Name,
                partNumber,
                request.SKU,
                request.CategoryId,
                request.BrandId,
                request.BaseUnitId,
                request.UnitId,
                request.Description,
                request.RichDescription,
                request.CostPrice,
                request.SellingPrice,
                request.MinimumStock,
                request.HasWarranty,
                request.WarrantyPeriodMonths,
                request.WarrantyType,
                request.WarrantyTerms,
                request.WarrantyCertificateTemplate,
                request.Barcode,
                request.Tags,
                request.ProductType,
                request.IsPerishable,
                request.WeightKg,
                request.WidthCm,
                request.HeightCm,
                request.DepthCm,
                request.TaxCode
            );
            var currentUser = _currentUserService.GetCurrentUsername();
            part.CreatedBy = currentUser;
            part.ModifiedBy = currentUser;
            await _codeGenerateService.SaveGenerateCodeAsync("SKU", cancellationToken);
            await _partRepository.AddAsync(part, cancellationToken);

            // Record initial price in ProductVariantPriceHistory when selling price is set
            if (request.SellingPrice > 0)
            {
                try
                {
                    var initialPrice = ProductVariantPriceHistory.Create(
                        part.Id, request.SellingPrice, DateTime.UtcNow, null,
                        part.SellingPriceCurrency, "INITIAL_PRICE");
                    initialPrice.CreatedBy = currentUser;
                    initialPrice.ModifiedBy = currentUser;
                    await _variantPriceHistoryRepository.AddAsync(initialPrice, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create initial price history for part {PartId}", part.Id);
                }
            }

            var response = MapToResponse(part);
            return CreatedAtAction(nameof(GetById), new { id = part.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating part");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating part");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the part");
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PartResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdatePartRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.SKU) ||
                request.CategoryId == Guid.Empty)
                return BadRequest(new { message = "Name, SKU, and CategoryId are required" });

            _logger.LogInformation("Updating part: {PartId}", id);

            var part = await _partRepository.GetByIdAsync(id, cancellationToken);
            if (part is null)
            {
                _logger.LogWarning("Part not found for update: {PartId}", id);
                return NotFound(new { message = "Part not found" });
            }

            // Verify category exists
            if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
                return BadRequest(new { message = "Category does not exist" });

            // Check for duplicate SKU (excluding current part)
            if (await _partRepository.SKUExistsAsync(request.SKU, id, cancellationToken))
                return Conflict(new { message = $"Part SKU '{request.SKU}' already exists" });

            // Store old prices for history tracking
            var oldSellingPrice = part.SellingPrice;
            var oldCostPrice = part.CostPrice;
            var oldHasWarranty = part.HasWarranty;
            var oldWarrantyPeriodMonths = part.WarrantyPeriodMonths;
            var oldWarrantyType = part.WarrantyType;
            var oldWarrantyTerms = part.WarrantyTerms;
            var oldWarrantyCertificateTemplate = part.WarrantyCertificateTemplate;

            part.Update(request.Name, request.Description, request.SKU, request.CategoryId, request.BrandId,
                request.BaseUnitId, request.UnitId,
                request.CostPrice, request.SellingPrice, request.MinimumStock, request.IsActive,
                request.HasWarranty, request.WarrantyPeriodMonths, request.WarrantyType,
                request.WarrantyTerms, request.WarrantyCertificateTemplate,
                request.Barcode, request.Tags, request.ProductType,
                request.IsPerishable, request.WeightKg,
                request.WidthCm, request.HeightCm, request.DepthCm,
                request.TaxCode, request.RichDescription);
            part.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _partRepository.UpdateAsync(part, cancellationToken);

            var warrantySettingsChanged =
                oldHasWarranty != part.HasWarranty ||
                oldWarrantyPeriodMonths != part.WarrantyPeriodMonths ||
                !string.Equals(oldWarrantyType, part.WarrantyType, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldWarrantyTerms, part.WarrantyTerms, StringComparison.Ordinal) ||
                !string.Equals(oldWarrantyCertificateTemplate, part.WarrantyCertificateTemplate, StringComparison.Ordinal);

            if (warrantySettingsChanged)
            {
                await SyncWarrantyRegistrationsWithPartAsync(part, cancellationToken);
            }

            // Create price history records if selling price changed
            if (oldSellingPrice != request.SellingPrice)
            {
                var currentUser = _currentUserService.GetCurrentUsername();
                try
                {
                    // Simple audit log (old → new)
                    var priceHistory = PriceHistory.Create(
                        partId: part.Id, oldPrice: oldSellingPrice, newPrice: request.SellingPrice,
                        effectiveDate: DateTime.UtcNow, reason: "PRICE_UPDATE", changedBy: currentUser);
                    priceHistory.CreatedBy = currentUser;
                    priceHistory.ModifiedBy = currentUser;
                    await _priceHistoryRepository.AddAsync(priceHistory, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create price history for part {PartId}", part.Id);
                }

                // Sync ProductVariantPriceHistory (used by Pricing tab + price resolution)
                if (request.SellingPrice > 0)
                {
                    try
                    {
                        var vph = ProductVariantPriceHistory.Create(
                            part.Id, request.SellingPrice, DateTime.UtcNow, null,
                            part.SellingPriceCurrency, "PRICE_UPDATE");
                        vph.CreatedBy = currentUser;
                        vph.ModifiedBy = currentUser;
                        await _variantPriceHistoryRepository.SetNewPriceAsync(vph, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to sync variant price history for part {PartId}", part.Id);
                    }
                }
            }

            var response = MapToResponse(part);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating part");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating part: {PartId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the part");
        }
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PartResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Activating part: {PartId}", id);

            var part = await _partRepository.GetByIdAsync(id, cancellationToken);
            if (part is null)
            {
                _logger.LogWarning("Part not found for activation: {PartId}", id);
                return NotFound(new { message = "Part not found" });
            }

            part.Activate();
            part.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _partRepository.UpdateAsync(part, cancellationToken);

            var response = MapToResponse(part);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating part: {PartId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while activating the part");
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PartResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deactivating part: {PartId}", id);

            var part = await _partRepository.GetByIdAsync(id, cancellationToken);
            if (part is null)
            {
                _logger.LogWarning("Part not found for deactivation: {PartId}", id);
                return NotFound(new { message = "Part not found" });
            }

            part.Deactivate();
            part.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _partRepository.UpdateAsync(part, cancellationToken);

            var response = MapToResponse(part);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating part: {PartId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the part");
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting part: {PartId}", id);

            var exists = await _partRepository.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Part not found for deletion: {PartId}", id);
                return NotFound(new { message = "Part not found" });
            }

            await _partRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting part: {PartId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the part");
        }
    }

    // Vehicle Compatibility Endpoint
    [HttpGet("{partId:guid}/compatible-vehicles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompatibleVehicles(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting compatible vehicles for part: {PartId}", partId);

            if (!await _partRepository.ExistsAsync(partId, cancellationToken))
            {
                _logger.LogWarning("Part not found: {PartId}", partId);
                return NotFound(new { message = "Part not found" });
            }

            var compatibilities = await _compatibilityRepository.GetCompatibilitiesByPartAsync(partId, cancellationToken);
            var response = compatibilities.Select(c => new
            {
                Id = c.Id,
                VehicleId = c.VehicleId,
                VehicleMake = c.Vehicle?.Make ?? string.Empty,
                VehicleModel = c.Vehicle?.Model ?? string.Empty,
                VehicleYear = c.Vehicle?.Year ?? 0,
                VehicleEngineType = c.Vehicle?.EngineType ?? string.Empty,
                VehicleInfo = c.Vehicle != null ? $"{c.Vehicle.Make} {c.Vehicle.Model} {c.Vehicle.Year}" : string.Empty,
                IsCompatible = c.IsCompatible,
                Notes = c.Notes,
                CreatedBy = c.CreatedBy
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compatible vehicles for part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving compatible vehicles");
        }
    }

    private PartResponse MapToResponse(Part part)
    {
        return new PartResponse
        {
            Id = part.Id,
            Name = part.Name,
            Description = part.Description,
            RichDescription = part.RichDescription,
            PartNumber = part.PartNumber.Value,
            SKU = part.SKU,
            CategoryId = part.CategoryId,
            CategoryName = part.Category?.Name ?? string.Empty,
            BrandId = part.BrandId,
            BrandName = part.Brand?.Name,
            BrandCode = part.Brand?.Code,
            BaseUnitId = part.BaseUnitId,
            BaseUnitName = part.BaseUnit?.Name,
            BaseUnitCode = part.BaseUnit?.Code,
            UnitId = part.UnitId,
            UnitName = part.Unit?.Name,
            CostPrice = part.CostPrice,
            SellingPrice = part.SellingPrice,
            MinimumStock = part.MinimumStock,
            IsActive = part.IsActive,
            HasWarranty = part.HasWarranty,
            WarrantyPeriodMonths = part.WarrantyPeriodMonths,
            WarrantyType = part.WarrantyType,
            WarrantyTerms = part.WarrantyTerms,
            WarrantyCertificateTemplate = part.WarrantyCertificateTemplate,
            Barcode = part.Barcode,
            Tags = part.Tags,
            ProductType = part.ProductType,
            IsPerishable = part.IsPerishable,
            WeightKg = part.WeightKg,
            WidthCm = part.WidthCm,
            HeightCm = part.HeightCm,
            DepthCm = part.DepthCm,
            TaxCode = part.TaxCode,
            CreatedBy = part.CreatedBy,
            ModifiedBy = part.ModifiedBy
        };
    }

    private PartPublicResponse MapToPublicResponse(Part part)
    {
        return new PartPublicResponse
        {
            Id = part.Id,
            Name = part.Name,
            Description = part.Description,
            RichDescription = part.RichDescription,
            PartNumber = part.PartNumber.Value,
            SKU = part.SKU,
            CategoryId = part.CategoryId,
            CategoryName = part.Category?.Name ?? string.Empty,
            BrandId = part.BrandId,
            BrandName = part.Brand?.Name,
            BrandCode = part.Brand?.Code,
            BaseUnitId = part.BaseUnitId,
            BaseUnitName = part.BaseUnit?.Name,
            BaseUnitCode = part.BaseUnit?.Code,
            UnitId = part.UnitId,
            UnitName = part.Unit?.Name,
            SellingPrice = part.SellingPrice,
            MinimumStock = part.MinimumStock,
            IsActive = part.IsActive,
            HasWarranty = part.HasWarranty,
            WarrantyPeriodMonths = part.WarrantyPeriodMonths,
            WarrantyType = part.WarrantyType,
            WarrantyTerms = part.WarrantyTerms,
            WarrantyCertificateTemplate = part.WarrantyCertificateTemplate,
            Barcode = part.Barcode,
            Tags = part.Tags,
            ProductType = part.ProductType,
            IsPerishable = part.IsPerishable,
            WeightKg = part.WeightKg,
            WidthCm = part.WidthCm,
            HeightCm = part.HeightCm,
            DepthCm = part.DepthCm,
            TaxCode = part.TaxCode
        };
    }

    private async Task SyncWarrantyRegistrationsWithPartAsync(Part part, CancellationToken cancellationToken)
    {
        var warranties = (await _warrantyRegistrationRepository.GetByPartIdAsync(part.Id, cancellationToken)).ToList();
        if (warranties.Count == 0)
        {
            return;
        }

        var currentUser = _currentUserService.GetCurrentUsername();

        foreach (var warranty in warranties)
        {
            warranty.SyncFromPartWarranty(
                hasWarranty: part.HasWarranty,
                warrantyPeriodMonths: part.WarrantyPeriodMonths,
                warrantyType: part.WarrantyType,
                warrantyTerms: part.WarrantyTerms,
                warrantyCertificateTemplate: part.WarrantyCertificateTemplate,
                modifiedBy: currentUser,
                voidReason: "Part warranty was disabled during part update");

            await _warrantyRegistrationRepository.UpdateAsync(warranty, cancellationToken);
        }

        _logger.LogInformation(
            "Synchronized {Count} warranty registrations for part {PartId}",
            warranties.Count,
            part.Id);
    }
}
