using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.InventoryDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;
[Route("api/v1/[controller]")]
[ApiController]
[HasPermission(Permissions.InventoryView)]
public class StockLotMovementController : ControllerBase
{
    private readonly IStockLotMovementRepository _repository;
    private readonly IStockLotRepository _lotRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly ILogger<StockLotMovementController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public StockLotMovementController(IStockLotMovementRepository repository, IStockLotRepository lotRepository,
        IProductRepository productRepository, IStockLevelRepository stockLevelRepository,
        ICurrentUserService currentUserService, ILogger<StockLotMovementController> logger)
    {
        _repository = repository;
        _lotRepository = lotRepository;
        _productRepository = productRepository;
        _stockLevelRepository = stockLevelRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var movement = await _repository.GetByIdAsync(id, cancellationToken);
            if (movement is null) return NotFound();
            return Ok(await MapResponse(movement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock lot movement");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("lot/{stockLotId:guid}")]
    public async Task<IActionResult> GetByStockLot(Guid stockLotId, CancellationToken cancellationToken)
    {
        try
        {
            var movements = await _repository.GetByStockLotAsync(stockLotId, cancellationToken);
            var responses = await MapResponses(movements);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movements by lot");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("lot/{stockLotId:guid}/history")]
    public async Task<IActionResult> GetMovementHistory(Guid stockLotId, CancellationToken cancellationToken)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(stockLotId, cancellationToken);
            if (lot is null) return NotFound("Stock lot not found");
            var part = await _productRepository.GetByIdAsync(lot.PartId, cancellationToken);
            var movements = await _repository.GetByStockLotAsync(stockLotId, cancellationToken);
            var sortedMovements = movements.OrderBy(m => m.MovementDate).ToList();

            var movementItems = sortedMovements.Select(m => new StockLotMovementItem
            {
                MovementId = m.Id,
                Quantity = m.Quantity,
                MovementType = m.MovementType,
                MovementDate = m.MovementDate,
                CostAtMovement = m.CostAtMovement,
                MovementCost = m.GetMovementCost(),
                ReferenceType = m.ReferenceType,
                Reason = m.Reason
            }).ToList();

            return Ok(new StockLotMovementHistoryResponse
            {
                StockLotId = stockLotId,
                LotNumber = lot.LotNumber,
                PartId = lot.PartId,
                PartName = part?.Name ?? "",
                QuantityReceived = lot.QuantityReceived,
                QuantityAvailable = lot.QuantityAvailable,
                CostPrice = lot.CostPrice,
                Movements = movementItems.ToList(),
                TotalQuantityMoved = sortedMovements.Sum(m => m.Quantity),
                TotalMovementCost = sortedMovements.Sum(m => m.GetMovementCost())
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movement history");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("type/{movementType}")]
    public async Task<IActionResult> GetByMovementType(string movementType, CancellationToken cancellationToken)
    {
        try
        {
            var movements = await _repository.GetByMovementTypeAsync(movementType, cancellationToken);
            var responses = await MapResponses(movements);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movements by type");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("date-range")]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            var movements = await _repository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
            var responses = await MapResponses(movements);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movements by date range");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("sales/{stockLotId:guid}")]
    public async Task<IActionResult> GetSalesMovements(Guid stockLotId, CancellationToken cancellationToken)
    {
        try
        {
            var movements = await _repository.GetSalesMovementsAsync(stockLotId, cancellationToken);
            var responses = await MapResponses(movements);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales movements");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("summary/{partId:guid}")]
    public async Task<IActionResult> GetPartSummary(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var part = await _productRepository.GetByIdAsync(partId, cancellationToken);
            if (part is null) return NotFound("Part not found");

            var lots = await _lotRepository.GetByPartAsync(partId, cancellationToken);
            var lotItems = new List<LotSummaryItem>();

            foreach (var lot in lots)
            {
                var movements = await _repository.GetByStockLotAsync(lot.Id, cancellationToken);
                var salesQuantity = movements.Where(m => m.MovementType == "SALE").Sum(m => m.Quantity);

                lotItems.Add(new LotSummaryItem
                {
                    LotId = lot.Id,
                    LotNumber = lot.LotNumber,
                    SupplierId = lot.SupplierId,
                    SupplierName = "", // Could be loaded if needed
                    QuantityOnHand = lot.QuantityReceived,
                    QuantityAvailable = lot.QuantityAvailable,
                    CostPrice = lot.CostPrice,
                    LotCost = lot.GetAvailableCost(),
                    ReceivingDate = lot.ReceivingDate,
                    ExpiryDate = lot.ExpiryDate,
                    IsExpired = lot.IsExpired,
                    UnitsSold = salesQuantity
                });
            }

            return Ok(new StockLotSummaryResponse
            {
                PartId = partId,
                PartName = part.Name,
                PartSKU = part.SKU,
                Lots = lotItems,
                TotalQuantityOnHand = lots.Sum(l => l.QuantityReceived),
                TotalQuantityAvailable = lots.Sum(l => l.QuantityAvailable),
                TotalInventoryCost = lots.Sum(l => l.GetAvailableCost())
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting part summary");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    [HasPermission(Permissions.InventoryAdjustStock)]
    public async Task<IActionResult> Create(CreateStockLotMovementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(request.StockLotId, cancellationToken);
            if (lot is null) return NotFound("Stock lot not found");

            var movement = StockLotMovement.Create(request.StockLotId, request.Quantity, request.MovementType,
                request.ReferenceId, request.ReferenceType, request.MovementDate, request.CostAtMovement, request.Reason, request.Notes);

            var currentUser = _currentUserService.GetCurrentUsername();
            movement.CreatedBy = currentUser;
            movement.ModifiedBy = currentUser;

            var movementTypeUpper = request.MovementType.ToUpper();

            if (new[] { "SALE", "DAMAGE", "RETURN" }.Contains(movementTypeUpper))
            {
                lot.RemoveStock(request.Quantity, request.Quantity, request.Reason);
                lot.ModifiedBy = currentUser;
                await _lotRepository.UpdateAsync(lot, cancellationToken);

                var stockLevel = await _stockLevelRepository.GetByPartVariantAndWarehouseAsync(
                    lot.PartId, lot.VariantId, lot.WarehouseId, cancellationToken);
                if (stockLevel != null)
                {
                    stockLevel.RemoveStock(request.Quantity, request.Quantity,
                        $"Lot movement {request.MovementType}: {request.Reason}");
                    stockLevel.ModifiedBy = currentUser;
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);
                }
            }
            else if (new[] { "PURCHASE", "RECEIVE", "ADJUSTMENT" }.Contains(movementTypeUpper))
            {
                lot.AddStock(request.Quantity, request.Quantity, request.Reason);
                lot.ModifiedBy = currentUser;
                await _lotRepository.UpdateAsync(lot, cancellationToken);

                var stockLevel = await _stockLevelRepository.GetByPartVariantAndWarehouseAsync(
                    lot.PartId, lot.VariantId, lot.WarehouseId, cancellationToken);
                if (stockLevel != null)
                {
                    stockLevel.AddStock(request.Quantity, request.Quantity,
                        $"Lot movement {request.MovementType}: {request.Reason}");
                    stockLevel.ModifiedBy = currentUser;
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);
                }
            }

            await _repository.AddAsync(movement, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = movement.Id }, await MapResponse(movement));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock lot movement");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Maps a list without re-entering the DbContext per row.
    ///
    /// The previous form was Task.WhenAll(movements.Select(MapResponse)), which fired one lot
    /// lookup per movement *concurrently* on the scoped DbContext — EF rejects that with
    /// "A second operation was started on this context instance", so both list endpoints 500'd on
    /// any non-empty result. Resolving the distinct lots once also removes the N+1.
    /// </summary>
    private async Task<List<StockLotMovementResponse>> MapResponses(
        IEnumerable<StockLotMovement> movements, CancellationToken cancellationToken = default)
    {
        var list = movements.ToList();
        var lotNumbers = new Dictionary<Guid, string>();

        foreach (var lotId in list.Select(m => m.StockLotId).Distinct())
        {
            var lot = await _lotRepository.GetByIdAsync(lotId, cancellationToken);
            if (lot is not null) lotNumbers[lotId] = lot.LotNumber;
        }

        return list.Select(m => MapResponse(m, lotNumbers.GetValueOrDefault(m.StockLotId, ""))).ToList();
    }

    private async Task<StockLotMovementResponse> MapResponse(StockLotMovement movement)
    {
        var lot = await _lotRepository.GetByIdAsync(movement.StockLotId);
        return MapResponse(movement, lot?.LotNumber ?? "");
    }

    private static StockLotMovementResponse MapResponse(StockLotMovement movement, string lotNumber)
    {
        return new StockLotMovementResponse
        {
            Id = movement.Id,
            StockLotId = movement.StockLotId,
            LotNumber = lotNumber,
            Quantity = movement.Quantity,
            QuantityInBaseUnit = movement.QuantityInBaseUnit,
            UnitId = movement.UnitId,
            UnitName = movement.Unit?.Name,
            UnitSymbol = movement.Unit?.Symbol,
            MovementType = movement.MovementType,
            ReferenceId = movement.ReferenceId,
            ReferenceType = movement.ReferenceType,
            MovementDate = movement.MovementDate,
            CostAtMovement = movement.CostAtMovement,
            CostAtMovementInBaseUnit = movement.CostAtMovementInBaseUnit,
            MovementCost = movement.GetMovementCost(),
            Reason = movement.Reason,
            Notes = movement.Notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
