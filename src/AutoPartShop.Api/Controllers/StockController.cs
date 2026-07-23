using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.StockDtos;
using AutoPartShop.Application.Services;
using AutoPartShop.Application.Stock;
using AutoPartShop.Application.Stock.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.InventoryView)]
public class StockController : ControllerBase
{
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStockLevelReadRepository _stockLevelReadRepository;
    private readonly IStockMovementReadRepository _stockMovementReadRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitConversionService _unitConversionService;
    private readonly StockAdjustmentApplier _adjustmentApplier;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ILogger<StockController> _logger;

    public StockController(
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        IStockLevelReadRepository stockLevelReadRepository,
        IStockMovementReadRepository stockMovementReadRepository,
        AutoPartDbContext dbContext,
        ICurrentUserService currentUserService,
        IUnitConversionService unitConversionService,
        StockAdjustmentApplier adjustmentApplier,
        ICodeGenerateService codeGenerateService,
        ILogger<StockController> logger)
    {
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _stockLevelReadRepository = stockLevelReadRepository;
        _stockMovementReadRepository = stockMovementReadRepository;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _unitConversionService = unitConversionService;
        _adjustmentApplier = adjustmentApplier;
        _codeGenerateService = codeGenerateService;
        _logger = logger;
    }

    /// <summary>
    /// POS quick-sale availability probe: returns net available stock (on-hand minus reserved,
    /// in base units) for a single part and whether the requested quantity can be met.
    /// </summary>
    [HttpPost("check")]
    public async Task<IActionResult> CheckStock([FromBody] StockCheckRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.PartId == Guid.Empty)
            return BadRequest(new { message = "PartId is required" });

        try
        {
            var response = await CheckStockInternalAsync(request.PartId, request.VariantId, request.Quantity, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock for part {PartId}", request.PartId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while checking stock");
        }
    }

    /// <summary>Batch variant of <see cref="CheckStock"/> â€” one response per requested part.</summary>
    [HttpPost("check-multiple")]
    public async Task<IActionResult> CheckMultipleStock([FromBody] List<StockCheckRequest> requests, CancellationToken cancellationToken)
    {
        if (requests is null || requests.Count == 0)
            return BadRequest(new { message = "At least one stock check request is required" });

        try
        {
            var responses = new List<StockCheckResponse>(requests.Count);
            foreach (var request in requests)
                responses.Add(await CheckStockInternalAsync(request.PartId, request.VariantId, request.Quantity, cancellationToken));

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock for multiple parts");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while checking stock");
        }
    }

    private async Task<StockCheckResponse> CheckStockInternalAsync(Guid partId, Guid? variantId, int quantity, CancellationToken cancellationToken)
    {
        // When a variant is specified, check only that SKU's stock; otherwise sum the part's levels.
        var stockLevels = (variantId.HasValue
            ? await _stockLevelRepository.GetByPartAndVariantAsync(partId, variantId, cancellationToken)
            : await _stockLevelRepository.GetByPartAsync(partId, cancellationToken)).ToList();

        // Net available in base units: prefer base-unit columns, fall back to display-unit columns.
        var totalAvailable = stockLevels.Sum(sl =>
            (sl.QuantityOnHandInBaseUnit > 0 ? sl.QuantityOnHandInBaseUnit : sl.QuantityOnHand) -
            (sl.QuantityReservedInBaseUnit > 0 ? sl.QuantityReservedInBaseUnit : sl.QuantityReserved));

        var available = totalAvailable >= quantity;

        return new StockCheckResponse
        {
            PartId = partId,
            VariantId = variantId,
            StockAvailable = totalAvailable,
            Available = available,
            Message = available
                ? null
                : $"Insufficient stock. Available: {totalAvailable}, Required: {quantity}"
        };
    }

    [HttpGet("levels")]
    public async Task<IActionResult> GetAllStockLevels(CancellationToken cancellationToken)
    {
        try
        {
            var levels = await _stockLevelRepository.GetAllAsync(cancellationToken);
            var response = levels.Select(MapToStockLevelResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all stock levels");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving stock levels");
        }
    }

    [HttpPost("levels/list")]
    public async Task<IActionResult> GetStockLevelsList([FromBody] StockLevelQuery? query, CancellationToken cancellationToken = default)
    {
        if (query is null)
            return BadRequest("Request body is required.");

        if (query.PageNumber < 1)
            return BadRequest("PageNumber must be greater than 0.");

        if (query.PageSize < 1)
            return BadRequest("PageSize must be greater than 0.");

        try
        {
            var (levels, totalCount) =
                await _stockLevelReadRepository.FindAllQuery(query, cancellationToken);

            var result = PagedResult<StockLevelResponse>
                .Create(levels, totalCount, query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock levels list");
            return StatusCode(500, "An error occurred while retrieving stock levels");
        }
    }

    [HttpGet("levels/{id:guid}")]
    public async Task<IActionResult> GetStockLevelById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var level = await _stockLevelRepository.GetByIdAsync(id, cancellationToken);
            if (level is null) return NotFound(new { message = "Stock level not found" });

            return Ok(MapToStockLevelResponse(level));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock level by ID: {StockLevelId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the stock level");
        }
    }

    [HttpGet("levels/part/{partId:guid}")]
    public async Task<IActionResult> GetStockLevelsByPart(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var levels = await _stockLevelRepository.GetByPartAsync(partId, cancellationToken);
            var response = levels.Select(MapToStockLevelResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock levels by part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving stock levels");
        }
    }

    [HttpGet("levels/warehouse/{warehouseId:guid}")]
    public async Task<IActionResult> GetStockLevelsByWarehouse(Guid warehouseId, CancellationToken cancellationToken)
    {
        try
        {
            var levels = await _stockLevelRepository.GetByWarehouseAsync(warehouseId, cancellationToken);
            var response = levels.Select(MapToStockLevelResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock levels by warehouse: {WarehouseId}", warehouseId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving stock levels");
        }
    }

    [HttpGet("levels/low-stock")]
    public async Task<IActionResult> GetLowStock(CancellationToken cancellationToken)
    {
        try
        {
            var levels = await _stockLevelRepository.GetLowStockAsync(cancellationToken);
            var response = levels.Select(MapToStockLevelResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock items");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving low stock items");
        }
    }

    [HttpGet("levels/part/{partId:guid}/warehouse/{warehouseId:guid}")]
    public async Task<IActionResult> GetStockLevelByPartAndWarehouse(Guid partId, Guid warehouseId, CancellationToken cancellationToken)
    {
        try
        {
            var level = await _stockLevelRepository.GetByPartAndWarehouseAsync(partId, warehouseId, cancellationToken);
            if (level is null) return NotFound(new { message = "Stock level not found" });

            return Ok(MapToStockLevelResponse(level));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock level by part and warehouse");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the stock level");
        }
    }

    [HttpPost("levels")]
    [HasPermission(Permissions.InventoryAdjustStock)]
    public async Task<IActionResult> CreateStockLevel(CreateStockLevelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PartId == Guid.Empty || request.WarehouseId == Guid.Empty)
                return BadRequest(new { message = "PartId and WarehouseId are required" });

            var stockLevel = StockLevel.Create(
                request.PartId,
                request.WarehouseId,
                request.ReorderLevel,
                request.ReorderQuantity,
                variantId: request.VariantId
            );
            stockLevel.CreatedBy = _currentUserService.GetCurrentUsername();
            stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _stockLevelRepository.AddAsync(stockLevel, cancellationToken);

            return CreatedAtAction(nameof(GetStockLevelById), new { id = stockLevel.Id }, MapToStockLevelResponse(stockLevel));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock level");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the stock level");
        }
    }

    [HttpPut("levels/{id:guid}")]
    [HasPermission(Permissions.InventoryAdjustStock)]
    public async Task<IActionResult> UpdateStockLevel(Guid id, UpdateStockLevelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var level = await _stockLevelRepository.GetByIdAsync(id, cancellationToken);
            if (level is null) return NotFound(new { message = "Stock level not found" });

            level.UpdateReorderLevel(request.ReorderLevel, request.ReorderQuantity);
            level.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _stockLevelRepository.UpdateAsync(level, cancellationToken);

            return Ok(MapToStockLevelResponse(level));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock level: {StockLevelId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the stock level");
        }
    }

    [HttpGet("movements")]
    public async Task<IActionResult> GetAllMovements(CancellationToken cancellationToken)
    {
        try
        {
            var movements = await _stockMovementRepository.GetAllAsync(cancellationToken);
            var response = movements.Select(MapToStockMovementResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all stock movements");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving stock movements");
        }
    }

    [HttpPost("movements/list")]
    public async Task<IActionResult> GetMovementsList(StockMovementQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null)
            return BadRequest("Request body is required.");

        if (query.PageNumber < 1)
            return BadRequest("PageNumber must be greater than 0.");

        if (query.PageSize < 1)
            return BadRequest("PageSize must be greater than 0.");

        try
        {
            var (movements, totalCount) =
                await _stockMovementReadRepository.FindAllQuery(query, cancellationToken);

            var result = PagedResult<StockMovementResponse>
                .Create(movements, totalCount, query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements list");
            return StatusCode(500, "An error occurred while retrieving stock movements");
        }
    }

    [HttpGet("movements/{id:guid}")]
    public async Task<IActionResult> GetMovementById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var movement = await _stockMovementRepository.GetByIdAsync(id, cancellationToken);
            if (movement is null) return NotFound(new { message = "Movement not found" });

            return Ok(MapToStockMovementResponse(movement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movement by ID: {MovementId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the movement");
        }
    }

    [HttpPost("movements")]
    [HasPermission(Permissions.InventoryAdjustStock)]
    public async Task<IActionResult> CreateMovement(CreateStockMovementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PartId == Guid.Empty)
                return BadRequest(new { message = "PartId is required" });

            // Note: In production, we would need to get the StockLevelId from PartId and WarehouseId
            // For now, this is a simplified version
            var movement = StockMovement.Create(
                Guid.NewGuid(),
                request.Type,
                request.Quantity,
                request.Reference,
                request.Reference
            );
            movement.CreatedBy = _currentUserService.GetCurrentUsername();
            movement.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _stockMovementRepository.AddAsync(movement, cancellationToken);

            return CreatedAtAction(nameof(GetMovementById), new { id = movement.Id }, MapToStockMovementResponse(movement));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock movement");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the stock movement");
        }
    }

    [HttpPost("transfer")]
    [HasPermission(Permissions.InventoryAdjustStock)]
    public async Task<IActionResult> TransferStock(StockTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (request.PartId == Guid.Empty)
                return BadRequest(new { message = "PartId is required" });
            if (request.FromWarehouseId == Guid.Empty)
                return BadRequest(new { message = "FromWarehouseId is required" });
            if (request.ToWarehouseId == Guid.Empty)
                return BadRequest(new { message = "ToWarehouseId is required" });
            if (request.FromWarehouseId == request.ToWarehouseId)
                return BadRequest(new { message = "Source and destination warehouses must be different" });
            if (request.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero" });

            // Check if part exists
            var part = await _dbContext.Parts
                .FirstOrDefaultAsync(p => p.Id == request.PartId && !p.Isdeleted, cancellationToken);
            if (part == null)
                return NotFound(new { message = "Part not found" });

            // Check if source warehouse exists
            var fromWarehouse = await _dbContext.Set<Warehouse>()
                .FirstOrDefaultAsync(w => w.Id == request.FromWarehouseId && !w.Isdeleted, cancellationToken);
            if (fromWarehouse == null)
                return NotFound(new { message = "Source warehouse not found" });

            // Check if destination warehouse exists
            var toWarehouse = await _dbContext.Set<Warehouse>()
                .FirstOrDefaultAsync(w => w.Id == request.ToWarehouseId && !w.Isdeleted, cancellationToken);
            if (toWarehouse == null)
                return NotFound(new { message = "Destination warehouse not found" });

            // Get or create source stock level
            var sourceStockLevel = await _stockLevelRepository.GetByPartVariantAndWarehouseAsync(
                request.PartId, request.VariantId, request.FromWarehouseId, cancellationToken);

            if (sourceStockLevel == null)
                return BadRequest(new { message = "Part not available in source warehouse" });

            // Check if enough stock is available (use base unit for comparison)
            int quantityInBaseUnit = request.QuantityInBaseUnit;
            if (request.UnitId.HasValue && part.UnitId.HasValue && request.UnitId != part.UnitId)
            {
                // Convert from display unit to base unit
                var conversionFactor = await _unitConversionService.GetConversionFactorAsync(
                    request.UnitId.Value, part.UnitId.Value);
                quantityInBaseUnit = (int)Math.Round(request.Quantity * conversionFactor);
            }
            else if (!request.UnitId.HasValue)
            {
                // If no unit specified, assume display quantity equals base quantity
                quantityInBaseUnit = request.Quantity;
            }

            if (sourceStockLevel.QuantityAvailableInBaseUnit < quantityInBaseUnit)
                return BadRequest(new { message = $"Insufficient stock. Available: {sourceStockLevel.QuantityAvailableInBaseUnit}, Requested: {quantityInBaseUnit}" });

            var transferReference = string.IsNullOrEmpty(request.Reference)
                ? $"TRANSFER-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : request.Reference;
            var currentUser = _currentUserService.GetCurrentUsername();

            // Everything below mutates stock — level buckets, movements AND lots — so it must be
            // atomic. Without a transaction a mid-way failure could remove from the source but never
            // add to the destination (stock destroyed), or move levels without moving lots (making the
            // transferred stock unsellable at the destination, since sales deduct FIFO from lots).
            Guid outMovementId = Guid.Empty;
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Re-fetch tracked entities inside the transaction so a retry works on clean state
                    // and the RowVersion check reflects the latest committed values.
                    var source = await _dbContext.StockLevels
                        .FirstOrDefaultAsync(sl => sl.Id == sourceStockLevel.Id, cancellationToken)
                        ?? throw new InvalidOperationException("Source stock level not found");

                    if (source.QuantityAvailableInBaseUnit < quantityInBaseUnit)
                        throw new InvalidOperationException(
                            $"Insufficient stock. Available: {source.QuantityAvailableInBaseUnit}, Requested: {quantityInBaseUnit}");

                    var dest = await _dbContext.StockLevels
                        .FirstOrDefaultAsync(sl => sl.PartId == request.PartId && sl.VariantId == request.VariantId
                                                && sl.WarehouseId == request.ToWarehouseId, cancellationToken);
                    if (dest == null)
                    {
                        dest = StockLevel.Create(request.PartId, request.ToWarehouseId, 0, 0, variantId: request.VariantId);
                        dest.CreatedBy = currentUser;
                        dest.ModifiedBy = currentUser;
                        _dbContext.StockLevels.Add(dest);
                    }

                    // ── Move stock LOTS (FIFO), preserving each cost layer ──────────────────────────
                    // Draw down source lots oldest-first and create matching destination lots with the
                    // same cost/expiry/batch/warranty/provenance, so the destination is sellable and
                    // FIFO cost integrity is preserved on both ends.
                    var sourceLots = await _dbContext.StockLots
                        .Where(l => l.PartId == request.PartId
                                 && l.VariantId == request.VariantId
                                 && l.WarehouseId == request.FromWarehouseId
                                 && l.Status == "AVAILABLE"
                                 && l.QuantityAvailableInBaseUnit > 0
                                 && !l.Isdeleted)
                        .OrderBy(l => l.ExpiryDate)
                        .ThenBy(l => l.CreatedDate)
                        .ToListAsync(cancellationToken);

                    int remainingBase = quantityInBaseUnit;
                    foreach (var srcLot in sourceLots)
                    {
                        if (remainingBase <= 0) break;
                        int drawBase = Math.Min(srcLot.QuantityAvailableInBaseUnit, remainingBase);

                        srcLot.RemoveStock(drawBase, drawBase, $"Transfer to {toWarehouse.Name}");
                        srcLot.ModifiedBy = currentUser;

                        var srcLotMovement = StockLotMovement.Create(
                            srcLot.Id, drawBase, "TRANSFER", null, "StockTransfer",
                            DateTime.UtcNow, srcLot.CostPrice, $"Transfer to {toWarehouse.Name}",
                            transferReference, srcLot.UnitId, drawBase, srcLot.CostPriceInBaseUnit);
                        srcLotMovement.CreatedBy = currentUser;
                        srcLotMovement.ModifiedBy = currentUser;
                        _dbContext.StockLotMovements.Add(srcLotMovement);

                        var destLotNumber = await _codeGenerateService.GenerateAsync("LOT", cancellationToken);
                        var destLot = StockLot.Create(
                            lotNumber: destLotNumber,
                            partId: srcLot.PartId,
                            warehouseId: request.ToWarehouseId,
                            supplierId: srcLot.SupplierId,
                            goodsReceiptLineId: srcLot.GoodsReceiptLineId,
                            quantityReceived: drawBase,
                            costPrice: srcLot.CostPrice,
                            receivingDate: srcLot.ReceivingDate,
                            manufacturerLotNumber: srcLot.ManufacturerLotNumber,
                            expiryDate: srcLot.ExpiryDate,
                            currency: srcLot.Currency,
                            notes: $"Transferred from {fromWarehouse.Name} lot {srcLot.LotNumber} ({transferReference})",
                            unitId: srcLot.UnitId,
                            quantityReceivedInBaseUnit: drawBase,
                            costPriceInBaseUnit: srcLot.CostPriceInBaseUnit,
                            hasWarranty: srcLot.HasWarranty,
                            warrantyPeriodMonths: srcLot.WarrantyPeriodMonths,
                            warrantyType: srcLot.WarrantyType,
                            warrantyTerms: srcLot.WarrantyTerms,
                            variantId: srcLot.VariantId,
                            status: "AVAILABLE");
                        destLot.CreatedBy = currentUser;
                        destLot.ModifiedBy = currentUser;
                        _dbContext.StockLots.Add(destLot);

                        var destLotMovement = StockLotMovement.Create(
                            destLot.Id, drawBase, "TRANSFER", null, "StockTransfer",
                            DateTime.UtcNow, destLot.CostPrice, $"Transfer from {fromWarehouse.Name}",
                            transferReference, destLot.UnitId, drawBase, destLot.CostPriceInBaseUnit);
                        destLotMovement.CreatedBy = currentUser;
                        destLotMovement.ModifiedBy = currentUser;
                        _dbContext.StockLotMovements.Add(destLotMovement);

                        remainingBase -= drawBase;
                    }

                    if (remainingBase > 0)
                        throw new InvalidOperationException(
                            $"Insufficient lot stock in source warehouse: on-hand level is sufficient but lot records are short by {remainingBase} base units. Run a stock reconciliation.");

                    // ── Level bucket movements (audit) ─────────────────────────────────────────────
                    var outMovement = StockMovement.Create(
                        source.Id, "TRANSFER", -request.Quantity, transferReference,
                        $"Transfer to {toWarehouse.Name}", unitId: request.UnitId, quantityInBaseUnit: -quantityInBaseUnit);
                    outMovement.CreatedBy = currentUser;
                    outMovement.ModifiedBy = currentUser;
                    outMovement.Approve("System");
                    _dbContext.StockMovements.Add(outMovement);
                    outMovementId = outMovement.Id;

                    var inMovement = StockMovement.Create(
                        dest.Id, "TRANSFER", request.Quantity, transferReference,
                        $"Transfer from {fromWarehouse.Name}", unitId: request.UnitId, quantityInBaseUnit: quantityInBaseUnit);
                    inMovement.CreatedBy = currentUser;
                    inMovement.ModifiedBy = currentUser;
                    inMovement.Approve("System");
                    _dbContext.StockMovements.Add(inMovement);

                    // ── Aggregate level buckets ────────────────────────────────────────────────────
                    source.RemoveStock(request.Quantity, quantityInBaseUnit, "Transfer");
                    source.ModifiedBy = currentUser;

                    dest.AddStock(request.Quantity, quantityInBaseUnit, "Transfer");
                    dest.ModifiedBy = currentUser;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            // Return transfer response
            var response = new StockTransferResponse
            {
                Id = outMovementId,
                PartId = part.Id,
                PartName = part.Name,
                PartCode = part.PartNumber?.Value ?? part.SKU,
                FromWarehouseId = fromWarehouse.Id,
                FromWarehouseName = fromWarehouse.Name,
                FromWarehouseCode = fromWarehouse.Code,
                ToWarehouseId = toWarehouse.Id,
                ToWarehouseName = toWarehouse.Name,
                ToWarehouseCode = toWarehouse.Code,
                Quantity = request.Quantity,
                QuantityInBaseUnit = quantityInBaseUnit,
                UnitId = request.UnitId,
                Reference = transferReference,
                Notes = request.Notes,
                Status = "COMPLETED",
                CreatedBy = _currentUserService.GetCurrentUsername(),
                CreatedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule failures raised inside the transaction (insufficient stock, lot shortfall).
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring stock");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while transferring stock");
        }
    }

    [HttpPost("adjust")]
    [HasPermission(Permissions.InventoryAdjustStock)]
    public async Task<IActionResult> AdjustStock(StockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (request.PartId == Guid.Empty)
                return BadRequest(new { message = "PartId is required" });
            if (request.WarehouseId == Guid.Empty)
                return BadRequest(new { message = "WarehouseId is required" });
            if (request.Quantity == 0)
                return BadRequest(new { message = "Quantity cannot be zero" });
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { message = "Reason is required" });

            // Check if part exists
            var part = await _dbContext.Parts
                .FirstOrDefaultAsync(p => p.Id == request.PartId && !p.Isdeleted, cancellationToken);
            if (part == null)
                return NotFound(new { message = "Part not found" });

            // Check if warehouse exists
            var warehouse = await _dbContext.Set<Warehouse>()
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && !w.Isdeleted, cancellationToken);
            if (warehouse == null)
                return NotFound(new { message = "Warehouse not found" });

            // Get or create stock level
            var stockLevel = await _stockLevelRepository.GetByPartVariantAndWarehouseAsync(
                request.PartId, request.VariantId, request.WarehouseId, cancellationToken);

            if (stockLevel == null)
            {
                stockLevel = StockLevel.Create(
                    request.PartId,
                    request.WarehouseId,
                    0,
                    0,
                    variantId: request.VariantId
                );
                stockLevel.CreatedBy = _currentUserService.GetCurrentUsername();
                stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
                await _stockLevelRepository.AddAsync(stockLevel, cancellationToken);
            }

            var previousQuantity = stockLevel.QuantityOnHand;
            var previousQuantityBase = stockLevel.QuantityOnHandInBaseUnit;

            // Calculate quantity in base unit
            int quantityInBaseUnit = request.QuantityInBaseUnit;
            if (request.UnitId.HasValue && part.UnitId.HasValue && request.UnitId != part.UnitId)
            {
                // Convert from display unit to base unit
                var conversionFactor = await _unitConversionService.GetConversionFactorAsync(
                    request.UnitId.Value, part.UnitId.Value);
                quantityInBaseUnit = (int)Math.Round(Math.Abs(request.Quantity) * conversionFactor);
                // Preserve sign (positive for increase, negative for decrease)
                quantityInBaseUnit = request.Quantity < 0 ? -quantityInBaseUnit : quantityInBaseUnit;
            }
            else if (!request.UnitId.HasValue)
            {
                // If no unit specified, assume display quantity equals base quantity
                quantityInBaseUnit = request.Quantity;
            }

            var adjustmentReference = string.IsNullOrEmpty(request.Reference)
                ? $"ADJUST-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : request.Reference;

            var username = _currentUserService.GetCurrentUsername();
            var stockLevelId = stockLevel.Id;

            // Apply level + movement + lot sync atomically. Lots must move with the level or lot
            // quantities drift from level quantities and lot-driven costing goes stale.
            // The execution strategy may retry the whole lambda on transient failures, so each
            // attempt clears the change tracker and reloads fresh state — otherwise a retry
            // would re-apply the delta to already-mutated tracked entities.
            StockAdjustmentApplier.AdjustmentOutcome outcome = null!;
            IActionResult? failure = null;
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                var level = await _dbContext.StockLevels
                    .FirstAsync(sl => sl.Id == stockLevelId, cancellationToken);

                previousQuantity = level.QuantityOnHand;
                previousQuantityBase = level.QuantityOnHandInBaseUnit;

                // Negative-stock guard on CURRENT values (inside the transaction, not the earlier read)
                if (request.Quantity < 0 && level.QuantityOnHandInBaseUnit + quantityInBaseUnit < 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    failure = BadRequest(new { message = $"Cannot reduce stock below zero. Current: {level.QuantityOnHandInBaseUnit}, Adjustment: {quantityInBaseUnit}" });
                    return;
                }

                outcome = await _adjustmentApplier.ApplyAsync(
                    level,
                    request.Quantity,
                    quantityInBaseUnit,
                    request.Reason,
                    adjustmentReference,
                    request.Notes,
                    username,
                    unitId: request.UnitId,
                    cancellationToken: cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                stockLevel = level;
            });

            if (failure != null)
                return failure;

            if (outcome.LotSyncSkipped)
                _logger.LogWarning(
                    "Stock adjustment {Reference} applied to level but lots only partially synced (part {PartId}, warehouse {WarehouseId}) — pre-existing lot/level drift or no lot to receive found stock",
                    adjustmentReference, request.PartId, request.WarehouseId);

            var newQuantity = stockLevel.QuantityOnHand;
            var newQuantityBase = stockLevel.QuantityOnHandInBaseUnit;
            var movement = outcome.Movement;

            // Return adjustment response
            var response = new StockAdjustmentResponse
            {
                Id = movement.Id,
                PartId = part.Id,
                PartName = part.Name,
                PartCode = part.PartNumber?.Value ?? part.SKU,
                WarehouseId = warehouse.Id,
                WarehouseName = warehouse.Name,
                WarehouseCode = warehouse.Code,
                Quantity = request.Quantity,
                QuantityInBaseUnit = quantityInBaseUnit,
                UnitId = request.UnitId,
                PreviousQuantity = previousQuantity,
                PreviousQuantityInBaseUnit = previousQuantityBase,
                NewQuantity = newQuantity,
                NewQuantityInBaseUnit = newQuantityBase,
                Reason = request.Reason,
                Reference = adjustmentReference,
                Notes = request.Notes,
                CreatedBy = _currentUserService.GetCurrentUsername(),
                CreatedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Domain guard (e.g. reserved stock blocks the decrease) — the transaction rolled back.
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adjusting stock");
        }
    }

    private StockLevelResponse MapToStockLevelResponse(StockLevel level)
    {
        return new StockLevelResponse
        {
            Id = level.Id,
            PartId = level.PartId,
            PartName = level.Part?.Name,
            PartLocalName = level.Part?.LocalName,
            PartSku = level.Part?.SKU,
            VariantId = level.VariantId,
            VariantName = level.Variant?.Name,
            VariantSku = level.Variant?.SKU,
            WarehouseId = level.WarehouseId,
            WarehouseName = level.Warehouse?.Name,
            UnitId = level.UnitId,
            UnitName = level.Unit?.Name,
            UnitSymbol = level.Unit?.Symbol,
            BaseUnitName = level.Part?.BaseUnit?.Name,
            BaseUnitSymbol = level.Part?.BaseUnit?.Symbol,
            Quantity = level.QuantityOnHand,
            QuantityInBaseUnit = level.QuantityOnHandInBaseUnit,
            ReservedQuantity = level.QuantityReserved,
            ReservedQuantityInBaseUnit = level.QuantityReservedInBaseUnit,
            AvailableQuantity = level.QuantityAvailable,
            AvailableQuantityInBaseUnit = level.QuantityAvailableInBaseUnit,
            ReorderLevel = level.ReorderLevel,
            ReorderQuantity = level.ReorderQuantity,
            // Status is calculated based on BASE UNIT quantities for accuracy
            NeedsReorder = level.QuantityAvailableInBaseUnit <= level.ReorderLevel,
            CreatedAt = DateTime.UtcNow
        };
    }

    private StockMovementResponse MapToStockMovementResponse(StockMovement movement)
    {
        var part = movement.StockLevel?.Part;
        var warehouse = movement.StockLevel?.Warehouse;

        return new StockMovementResponse
        {
            Id = movement.Id,
            PartId = movement.StockLevel?.PartId ?? Guid.Empty,
            PartName = part?.Name ?? string.Empty,
            PartLocalName = part?.LocalName,
            PartCode = part?.PartNumber?.Value ?? part?.SKU ?? string.Empty,
            WarehouseId = movement.StockLevel?.WarehouseId ?? Guid.Empty,
            WarehouseName = warehouse?.Name ?? string.Empty,
            WarehouseCode = warehouse?.Code ?? string.Empty,
            Type = movement.MovementType,
            Quantity = movement.Quantity,
            QuantityInBaseUnit = movement.QuantityInBaseUnit,
            UnitId = movement.UnitId,
            UnitName = movement.Unit?.Name,
            UnitSymbol = movement.Unit?.Symbol,
            BaseUnitSymbol = movement.StockLevel?.Part?.BaseUnit?.Symbol,
            Reason = movement.Reason,
            Reference = movement.ReferenceNumber,
            Status = string.IsNullOrEmpty(movement.ApprovedBy) ? "PENDING" : "APPROVED",
            Notes = movement.Notes,
            ApprovedBy = movement.ApprovedBy,
            ApprovedAt = null,  // Not tracked in StockMovement
            CreatedAt = movement.MovementDate
        };
    }
}
