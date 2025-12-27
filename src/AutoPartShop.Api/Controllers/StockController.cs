using AutoPartShop.Application.DTOs.StockDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class StockController : ControllerBase
{
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ILogger<StockController> _logger;

    public StockController(IStockLevelRepository stockLevelRepository, IStockMovementRepository stockMovementRepository, ILogger<StockController> logger)
    {
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
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
            stockLevel.CreatedBy = "System";
            stockLevel.ModifiedBy = "System";

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
            level.ModifiedBy = "System";

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
            movement.CreatedBy = "System";
            movement.ModifiedBy = "System";

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
