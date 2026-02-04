using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.StockDtos;
using AutoPartShop.Application.Stock;
using AutoPartShop.Application.Stock.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class StockController : ControllerBase
{
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStockLevelReadRepository _stockLevelReadRepository;
    private readonly IStockMovementReadRepository _stockMovementReadRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<StockController> _logger;

    public StockController(
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        IStockLevelReadRepository stockLevelReadRepository,
        IStockMovementReadRepository stockMovementReadRepository,
        AutoPartDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<StockController> logger)
    {
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _stockLevelReadRepository = stockLevelReadRepository;
        _stockMovementReadRepository = stockMovementReadRepository;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
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
    public async Task<IActionResult> GetStockLevelsList(StockLevelQuery query, CancellationToken cancellationToken = default)
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
                request.ReorderQuantity
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
            var sourceStockLevel = await _stockLevelRepository.GetByPartAndWarehouseAsync(
                request.PartId, request.FromWarehouseId, cancellationToken);

            if (sourceStockLevel == null)
                return BadRequest(new { message = "Part not available in source warehouse" });

            // Check if enough stock is available
            if (sourceStockLevel.QuantityAvailable < request.Quantity)
                return BadRequest(new { message = $"Insufficient stock. Available: {sourceStockLevel.QuantityAvailable}, Requested: {request.Quantity}" });

            // Get or create destination stock level
            var destStockLevel = await _stockLevelRepository.GetByPartAndWarehouseAsync(
                request.PartId, request.ToWarehouseId, cancellationToken);

            if (destStockLevel == null)
            {
                destStockLevel = StockLevel.Create(
                    request.PartId,
                    request.ToWarehouseId,
                    0,
                    0
                );
                destStockLevel.CreatedBy = _currentUserService.GetCurrentUsername();
                destStockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
                await _stockLevelRepository.AddAsync(destStockLevel, cancellationToken);
            }

            // Create stock movements for both warehouses
            var transferReference = string.IsNullOrEmpty(request.Reference)
                ? $"TRANSFER-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : request.Reference;

            // OUT movement from source warehouse
            var outMovement = StockMovement.Create(
                sourceStockLevel.Id,
                "TRANSFER",
                -request.Quantity,
                transferReference,
                $"Transfer to {toWarehouse.Name}"
            );
            outMovement.CreatedBy = _currentUserService.GetCurrentUsername();
            outMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
            outMovement.Approve("System");
            await _stockMovementRepository.AddAsync(outMovement, cancellationToken);

            // IN movement to destination warehouse
            var inMovement = StockMovement.Create(
                destStockLevel.Id,
                "TRANSFER",
                request.Quantity,
                transferReference,
                $"Transfer from {fromWarehouse.Name}"
            );
            inMovement.CreatedBy = _currentUserService.GetCurrentUsername();
            inMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
            inMovement.Approve("System");
            await _stockMovementRepository.AddAsync(inMovement, cancellationToken);

            // Update stock levels
            sourceStockLevel.RemoveStock(request.Quantity, "Transfer");
            sourceStockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _stockLevelRepository.UpdateAsync(sourceStockLevel, cancellationToken);

            destStockLevel.AddStock(request.Quantity, "Transfer");
            destStockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _stockLevelRepository.UpdateAsync(destStockLevel, cancellationToken);

            // Return transfer response
            var response = new StockTransferResponse
            {
                Id = outMovement.Id,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring stock");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while transferring stock");
        }
    }

    [HttpPost("adjust")]
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
            var stockLevel = await _stockLevelRepository.GetByPartAndWarehouseAsync(
                request.PartId, request.WarehouseId, cancellationToken);

            if (stockLevel == null)
            {
                stockLevel = StockLevel.Create(
                    request.PartId,
                    request.WarehouseId,
                    0,
                    0
                );
                stockLevel.CreatedBy = _currentUserService.GetCurrentUsername();
                stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
                await _stockLevelRepository.AddAsync(stockLevel, cancellationToken);
            }

            var previousQuantity = stockLevel.QuantityOnHand;

            // Check if negative adjustment would result in negative stock
            if (request.Quantity < 0 && stockLevel.QuantityOnHand + request.Quantity < 0)
                return BadRequest(new { message = $"Cannot reduce stock below zero. Current: {stockLevel.QuantityOnHand}, Adjustment: {request.Quantity}" });

            // Create stock adjustment movement
            var adjustmentReference = string.IsNullOrEmpty(request.Reference)
                ? $"ADJUST-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : request.Reference;

            var movement = StockMovement.Create(
                stockLevel.Id,
                "ADJUST",
                request.Quantity,
                adjustmentReference,
                $"{request.Reason}: {request.Notes}"
            );
            movement.CreatedBy = _currentUserService.GetCurrentUsername();
            movement.ModifiedBy = _currentUserService.GetCurrentUsername();
            movement.Approve("System");
            await _stockMovementRepository.AddAsync(movement, cancellationToken);

            // Update stock level
            if (request.Quantity > 0)
            {
                stockLevel.AddStock(request.Quantity, request.Reason);
            }
            else
            {
                stockLevel.RemoveStock(-request.Quantity, request.Reason);
            }
            stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

            var newQuantity = stockLevel.QuantityOnHand;

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
                PreviousQuantity = previousQuantity,
                NewQuantity = newQuantity,
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
            WarehouseId = level.WarehouseId,
            Quantity = level.QuantityOnHand,
            ReservedQuantity = level.QuantityReserved,
            AvailableQuantity = level.QuantityAvailable,
            ReorderLevel = level.ReorderLevel,
            ReorderQuantity = level.ReorderQuantity,
            NeedsReorder = level.NeedsReorder,
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
            PartCode = part?.PartNumber?.Value ?? part?.SKU ?? string.Empty,
            WarehouseId = movement.StockLevel?.WarehouseId ?? Guid.Empty,
            WarehouseName = warehouse?.Name ?? string.Empty,
            WarehouseCode = warehouse?.Code ?? string.Empty,
            Type = movement.MovementType,
            Quantity = movement.Quantity,
            Reference = movement.ReferenceNumber,
            Status = string.IsNullOrEmpty(movement.ApprovedBy) ? "PENDING" : "APPROVED",
            Notes = movement.Notes,
            ApprovedBy = movement.ApprovedBy,
            ApprovedAt = null,  // Not tracked in StockMovement
            CreatedAt = movement.MovementDate
        };
    }
}
