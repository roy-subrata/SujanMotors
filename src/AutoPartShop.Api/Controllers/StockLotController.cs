using AutoPartShop.Application.DTOs.InventoryDtos;
using AutoPartShop.Application.DTOs.PaymentDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StockLotController : ControllerBase
{
    private readonly IStockLotRepository _repository;
    private readonly IStockLotMovementRepository _movementRepository;
    private readonly IPartRepository _partRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<StockLotController> _logger;

    public StockLotController(IStockLotRepository repository, IStockLotMovementRepository movementRepository,
        IPartRepository partRepository, IWarehouseRepository warehouseRepository, ISupplierRepository supplierRepository,
        ILogger<StockLotController> logger)
    {
        _repository = repository;
        _movementRepository = movementRepository;
        _partRepository = partRepository;
        _warehouseRepository = warehouseRepository;
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var lot = await _repository.GetByIdAsync(id, cancellationToken);
            if (lot is null) return NotFound();
            return Ok(await MapResponse(lot));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock lot");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("by-lot/{lotNumber}")]
    public async Task<IActionResult> GetByLotNumber(string lotNumber, CancellationToken cancellationToken)
    {
        try
        {
            var lot = await _repository.GetByLotNumberAsync(lotNumber, cancellationToken);
            if (lot is null) return NotFound();
            return Ok(await MapResponse(lot));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock lot by number");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("part/{partId:guid}")]
    public async Task<IActionResult> GetByPart(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var lots = await _repository.GetByPartAsync(partId, cancellationToken);
            var responses = await Task.WhenAll(lots.Select(MapResponse));
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lots by part");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("price-history/{partId:guid}")]
    public async Task<IActionResult> GetPriceHistory(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var part = await _partRepository.GetByIdAsync(partId, cancellationToken);
            if (part is null) return NotFound("Part not found");

            var lots = await _repository.GetByPartAsync(partId, cancellationToken);
            var sortedLots = lots.OrderByDescending(l => l.ReceivingDate).ToList();

            // Supplier cache to avoid duplicate DB calls
            var supplierCache = new Dictionary<Guid, string>();

            var lotItems = new List<StockLotHistoryItem>();

            // All EF DB calls happen sequentially (safe)
            foreach (var l in sortedLots)
            {
                string supplierName;

                // If supplier already fetched, use cached value
                if (supplierCache.TryGetValue(l.SupplierId, out supplierName))
                {
                    // OK: supplierName loaded from cache
                }
                else
                {
                    // First time: load from DB (sync, safe)
                    var supplier = await _supplierRepository.GetByIdAsync(l.SupplierId, cancellationToken);
                    supplierName = supplier?.Name ?? "";

                    // Store in cache to avoid future DB calls
                    supplierCache[l.SupplierId] = supplierName;
                }

                lotItems.Add(new StockLotHistoryItem
                {
                    LotId = l.Id,
                    LotNumber = l.LotNumber,
                    SupplierId = l.SupplierId,
                    SupplierName = supplierName,
                    QuantityReceived = l.QuantityReceived,
                    QuantityAvailable = l.QuantityAvailable,
                    CostPrice = l.CostPrice,
                    ReceivingDate = l.ReceivingDate,
                    ExpiryDate = l.ExpiryDate,
                    IsExpired = l.IsExpired
                });
            }

            var prices = sortedLots.Select(l => l.CostPrice).ToList();

            return Ok(new StockLotPriceHistoryResponse
            {
                PartId = partId,
                PartName = part.Name,
                PartSKU = part.SKU,
                Lots = lotItems,
                MinPrice = prices.Any() ? prices.Min() : 0,
                MaxPrice = prices.Any() ? prices.Max() : 0,
                AveragePrice = prices.Any() ? prices.Average() : 0,
                LatestPrice = sortedLots.FirstOrDefault()?.CostPrice ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history");
            return StatusCode(500, "An error occurred");
        }
    }



    //[HttpGet("price-history/{partId:guid}")]
    //public async Task<IActionResult> GetPriceHistory(Guid partId, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        var part = await _partRepository.GetByIdAsync(partId, cancellationToken);
    //        if (part is null) return NotFound("Part not found");

    //        var lots = await _repository.GetByPartAsync(partId, cancellationToken);
    //        var sortedLots = lots.OrderByDescending(l => l.ReceivingDate).ToList();

    //        var lotItems = await Task.WhenAll(sortedLots.Select(async l => new StockLotHistoryItem
    //        {
    //            LotId = l.Id,
    //            LotNumber = l.LotNumber,
    //            SupplierId = l.SupplierId,
    //            SupplierName = (await _supplierRepository.GetByIdAsync(l.SupplierId, cancellationToken))?.Name ?? "",
    //            QuantityReceived = l.QuantityReceived,
    //            QuantityAvailable = l.QuantityAvailable,
    //            CostPrice = l.CostPrice,
    //            ReceivingDate = l.ReceivingDate,
    //            ExpiryDate = l.ExpiryDate,
    //            IsExpired = l.IsExpired
    //        }));

    //        var prices = sortedLots.Select(l => l.CostPrice).ToList();

    //        return Ok(new StockLotPriceHistoryResponse
    //        {
    //            PartId = partId,
    //            PartName = part.Name,
    //            PartSKU = part.SKU,
    //            Lots = lotItems.ToList(),
    //            MinPrice = prices.Any() ? prices.Min() : 0,
    //            MaxPrice = prices.Any() ? prices.Max() : 0,
    //            AveragePrice = prices.Any() ? prices.Average() : 0,
    //            LatestPrice = sortedLots.FirstOrDefault()?.CostPrice ?? 0
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error getting price history");
    //        return StatusCode(500, "An error occurred");
    //    }
    //}

    [HttpGet("warehouse/{partId:guid}/{warehouseId:guid}")]
    public async Task<IActionResult> GetByPartAndWarehouse(Guid partId, Guid warehouseId, CancellationToken cancellationToken)
    {
        try
        {
            var lots = await _repository.GetByPartAndWarehouseAsync(partId, warehouseId, cancellationToken);
            var responses = new List<StockLotResponse>();

            foreach (var lot in lots)
            {
                responses.Add(await MapResponse(lot));
            }
           // var responses = await Task.WhenAll(lots.Select(MapResponse));
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lots by part and warehouse");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("available/{partId:guid}/{warehouseId:guid}")]
    public async Task<IActionResult> GetAvailableLots(Guid partId, Guid warehouseId, CancellationToken cancellationToken)
    {
        try
        {
            var lots = await _repository.GetAvailableLotsAsync(partId, warehouseId, cancellationToken);
           
            var responses = new List<StockLotResponse>();
            foreach (var lot in lots)
            {
                responses.Add(await MapResponse(lot));
            }
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available lots");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("expired")]
    public async Task<IActionResult> GetExpiredLots(CancellationToken cancellationToken)
    {
        try
        {
            var lots = await _repository.GetExpiredLotsAsync(cancellationToken);
            var responses = await Task.WhenAll(lots.Select(MapResponse));
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired lots");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockLots(CancellationToken cancellationToken)
    {
        try
        {
            var lots = await _repository.GetLowStockLotsAsync(cancellationToken);
            var responses = await Task.WhenAll(lots.Select(MapResponse));
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock lots");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStockLotRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var lot = StockLot.Create(request.LotNumber, request.PartId, request.WarehouseId, request.SupplierId,
                request.GoodsReceiptLineId, request.QuantityReceived, request.CostPrice, request.ReceivingDate,
                request.ManufacturerLotNumber, request.ExpiryDate, request.Currency, request.Notes);

            lot.CreatedBy = "System";
            lot.ModifiedBy = "System";

            await _repository.AddAsync(lot, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = lot.Id }, await MapResponse(lot));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock lot");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStockLotRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var lot = await _repository.GetByIdAsync(id, cancellationToken);
            if (lot is null) return NotFound();

            if (request.ExpiryDate.HasValue)
                lot = StockLot.Create(lot.LotNumber, lot.PartId, lot.WarehouseId, lot.SupplierId, lot.GoodsReceiptLineId,
                    lot.QuantityReceived, lot.CostPrice, lot.ReceivingDate, request.ManufacturerLotNumber ?? lot.ManufacturerLotNumber,
                    request.ExpiryDate, lot.Currency, request.Notes ?? lot.Notes);
            else
                lot.UpdateNotes(request.Notes ?? lot.Notes);

            lot.ModifiedBy = "System";
            await _repository.UpdateAsync(lot, cancellationToken);
            return Ok(await MapResponse(lot));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock lot");
            return StatusCode(500, "An error occurred");
        }
    }

    private async Task<StockLotResponse> MapResponse(StockLot lot)
    {
        var part = await _partRepository.GetByIdAsync(lot.PartId);
        var warehouse = await _warehouseRepository.GetByIdAsync(lot.WarehouseId);
        var supplier = await _supplierRepository.GetByIdAsync(lot.SupplierId);

        return new StockLotResponse
        {
            Id = lot.Id,
            LotNumber = lot.LotNumber,
            PartId = lot.PartId,
            PartName = part?.Name ?? "",
            PartSKU = part?.SKU ?? "",
            WarehouseId = lot.WarehouseId,
            WarehouseName = warehouse?.Name ?? "",
            SupplierId = lot.SupplierId,
            SupplierName = supplier?.Name ?? "",
            QuantityReceived = lot.QuantityReceived,
            QuantityAvailable = lot.QuantityAvailable,
            CostPrice = lot.CostPrice,
            Currency = lot.Currency,
            TotalCost = lot.GetTotalCost(),
            AvailableCost = lot.GetAvailableCost(),
            ReceivingDate = lot.ReceivingDate,
            ExpiryDate = lot.ExpiryDate,
            IsExpired = lot.IsExpired,
            ManufacturerLotNumber = lot.ManufacturerLotNumber,
            Notes = lot.Notes,
            IsActive = lot.IsActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
