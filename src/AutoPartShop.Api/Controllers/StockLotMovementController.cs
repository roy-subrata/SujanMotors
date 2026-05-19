using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.InventoryDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StockLotMovementController : ControllerBase
{
    private readonly IStockLotMovementRepository _repository;
    private readonly IStockLotRepository _lotRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<StockLotMovementController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public StockLotMovementController(IStockLotMovementRepository repository, IStockLotRepository lotRepository,
        IProductRepository productRepository, ICurrentUserService currentUserService, ILogger<StockLotMovementController> logger)
    {
        _repository = repository;
        _lotRepository = lotRepository;
        _productRepository = productRepository;
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
            var responses = await Task.WhenAll(movements.Select(MapResponse));
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
            var responses = await Task.WhenAll(movements.Select(MapResponse));
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
            var responses = await Task.WhenAll(movements.Select(MapResponse));
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
            var responses = await Task.WhenAll(movements.Select(MapResponse));
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

            // Update lot quantity if it's a removal (SALE, DAMAGE, RETURN)
            if (new[] { "SALE", "DAMAGE", "RETURN" }.Contains(request.MovementType.ToUpper()))
            {
                lot.RemoveStock(request.Quantity, request.Quantity, request.Reason);
                lot.ModifiedBy = currentUser;
                await _lotRepository.UpdateAsync(lot, cancellationToken);
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

    private async Task<StockLotMovementResponse> MapResponse(StockLotMovement movement)
    {
        var lot = await _lotRepository.GetByIdAsync(movement.StockLotId);
        return new StockLotMovementResponse
        {
            Id = movement.Id,
            StockLotId = movement.StockLotId,
            LotNumber = lot?.LotNumber ?? "",
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
