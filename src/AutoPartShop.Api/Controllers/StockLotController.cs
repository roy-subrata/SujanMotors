using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.InventoryDtos;
using AutoPartShop.Application.Stock;
using AutoPartShop.Application.Stock.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class StockLotController(
    ILogger<StockLotController> _logger,
    IStockLotRepository _repository,
    IStockLotMovementRepository _movementRepository,
    IStockLotReadRepository _stockLotReadRepository,
    IProductRepository _productRepository,
    IWarehouseRepository _warehouseRepository,
    ISupplierRepository _supplierRepository,
    IUnitRepository _unitRepository,
    ICurrentUserService _currentUserService,
    AutoPartDbContext _dbContext
) : ControllerBase
{

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var lot = await _repository.GetByIdAsync(id, cancellationToken);
            if (lot is null) return NotFound();
            return Ok(await MapResponse(lot, cancellationToken));
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
            return Ok(await MapResponse(lot, cancellationToken));
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
            return Ok(await MapResponses(lots, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lots by part");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList(StockLotQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null)
            return BadRequest("Request body is required.");

        if (query.PageNumber < 1)
            return BadRequest("PageNumber must be greater than 0.");

        if (query.PageSize < 1)
            return BadRequest("PageSize must be greater than 0.");

        try
        {
            var (lots, totalCount) =
                await _stockLotReadRepository.FindAllQuery(query, cancellationToken);

            var result = PagedResult<StockLotResponse>
                .Create(lots, totalCount, query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock lots list");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("price-history/{partId:guid}")]
    public async Task<IActionResult> GetPriceHistory(
        Guid partId,
        [FromQuery] Guid? variantId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var part = await _productRepository.GetByIdAsync(partId, cancellationToken);
            if (part is null) return NotFound("Part not found");

            var lots = await _repository.GetByPartAsync(partId, cancellationToken);
            // Scope to a single variant when requested (SKU-level price history).
            if (variantId.HasValue && variantId.Value != Guid.Empty)
                lots = lots.Where(l => l.VariantId == variantId.Value).ToList();
            var sortedLots = lots.OrderByDescending(l => l.ReceivingDate).ToList();
            var totalCount = sortedLots.Count;
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            // Supplier cache to avoid duplicate DB calls
            var supplierCache = new Dictionary<Guid, string>();

            var lotItems = new List<StockLotHistoryItem>();

            // All EF DB calls happen sequentially (safe)
            foreach (var l in sortedLots
                         .Skip((pageNumber - 1) * pageSize)
                         .Take(pageSize))
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
                    HasWarranty = l.HasWarranty,
                    WarrantyPeriodMonths = l.WarrantyPeriodMonths,
                    WarrantyType = l.WarrantyType,
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
                LatestPrice = sortedLots.FirstOrDefault()?.CostPrice ?? 0,
                Pagination = new PaginationMeta
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
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
    //        var part = await _productRepository.GetByIdAsync(partId, cancellationToken);
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
            return Ok(await MapResponses(lots, cancellationToken));
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
            return Ok(await MapResponses(lots, cancellationToken));
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
            return Ok(await MapResponses(lots, cancellationToken));
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
            return Ok(await MapResponses(lots, cancellationToken));
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
            var lot = StockLot.Create(
                request.LotNumber, 
                request.PartId, 
                request.WarehouseId, 
                request.SupplierId,
                request.GoodsReceiptLineId, 
                request.QuantityReceived, 
                request.CostPrice, 
                request.ReceivingDate,
                request.ManufacturerLotNumber, 
                request.ExpiryDate, 
                request.Currency, 
                request.Notes,
                request.UnitId,
                request.QuantityReceivedInBaseUnit,
                request.CostPriceInBaseUnit,
                request.HasWarranty,
                request.WarrantyPeriodMonths,
                request.WarrantyType,
                request.WarrantyTerms);

            var currentUser = _currentUserService.GetCurrentUsername();
            lot.CreatedBy = currentUser;
            lot.ModifiedBy = currentUser;

            await _repository.AddAsync(lot, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = lot.Id }, await MapResponse(lot, cancellationToken));
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

            lot.UpdateDetails(request.ManufacturerLotNumber, request.ExpiryDate, request.Notes);

            if (request.HasWarranty.HasValue)
            {
                lot.UpdateWarranty(
                    request.HasWarranty ?? lot.HasWarranty,
                    request.WarrantyPeriodMonths ?? lot.WarrantyPeriodMonths,
                    request.WarrantyType ?? lot.WarrantyType,
                    request.WarrantyTerms ?? lot.WarrantyTerms);
            }

            lot.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(lot, cancellationToken);
            return Ok(await MapResponse(lot, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock lot");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Returns the oldest available lot (FIFO) selling price and warranty info for a part/warehouse.
    /// Used by the sales order form to pre-fill the default selling price.
    /// </summary>
    [HttpGet("fifo-info/{partId:guid}/{warehouseId:guid}")]
    public async Task<IActionResult> GetFifoInfo(Guid partId, Guid warehouseId, CancellationToken cancellationToken)
    {
        try
        {
            var fifoLot = await _dbContext.StockLots
                .Where(sl => sl.PartId == partId &&
                             sl.WarehouseId == warehouseId &&
                             sl.QuantityAvailableInBaseUnit > 0 &&
                             !sl.Isdeleted)
                .OrderBy(sl => sl.ExpiryDate == null ? 1 : 0)
                .ThenBy(sl => sl.ExpiryDate)
                .ThenBy(sl => sl.ReceivingDate)
                .ThenBy(sl => sl.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (fifoLot is null)
            {
                // Fall back to Part master data (no stock in this warehouse)
                var part = await _productRepository.GetByIdAsync(partId, cancellationToken);
                return Ok(new FifoLotInfoResponse
                {
                    HasAvailableLot = false,
                    SellingPrice = part?.SellingPrice ?? 0,
                    HasWarranty = part?.HasWarranty ?? false,
                    WarrantyPeriodMonths = part?.WarrantyPeriodMonths,
                    WarrantyType = part?.WarrantyType,
                    WarrantyTerms = part?.WarrantyTerms
                });
            }

            return Ok(new FifoLotInfoResponse
            {
                HasAvailableLot = true,
                LotId = fifoLot.Id,
                LotNumber = fifoLot.LotNumber,
                SellingPrice = (await _productRepository.GetByIdAsync(partId, cancellationToken))?.SellingPrice ?? 0,
                HasWarranty = fifoLot.HasWarranty,
                WarrantyPeriodMonths = fifoLot.WarrantyPeriodMonths,
                WarrantyType = fifoLot.WarrantyType,
                WarrantyTerms = fifoLot.WarrantyTerms,
                QuantityAvailable = fifoLot.QuantityAvailableInBaseUnit,
                ReceivingDate = fifoLot.ReceivingDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FIFO lot info for part {PartId} / warehouse {WarehouseId}", partId, warehouseId);
            return StatusCode(500, "An error occurred");
        }
    }

    private Task<StockLotResponse> MapResponse(StockLot lot, CancellationToken cancellationToken)
    {
        return MapResponse(
            lot,
            new Dictionary<Guid, Product?>(),
            new Dictionary<Guid, Warehouse?>(),
            new Dictionary<Guid, Supplier?>(),
            cancellationToken);
    }

    private async Task<List<StockLotResponse>> MapResponses(IEnumerable<StockLot> lots, CancellationToken cancellationToken)
    {
        var partCache = new Dictionary<Guid, Product?>();
        var warehouseCache = new Dictionary<Guid, Warehouse?>();
        var supplierCache = new Dictionary<Guid, Supplier?>();

        var responses = new List<StockLotResponse>();
        foreach (var lot in lots)
        {
            responses.Add(await MapResponse(lot, partCache, warehouseCache, supplierCache, cancellationToken));
        }
        return responses;
    }

    private async Task<StockLotResponse> MapResponse(
        StockLot lot,
        Dictionary<Guid, Product?> partCache,
        Dictionary<Guid, Warehouse?> warehouseCache,
        Dictionary<Guid, Supplier?> supplierCache,
        CancellationToken cancellationToken)
    {
        if (!partCache.TryGetValue(lot.PartId, out var part))
        {
            part = await _productRepository.GetByIdAsync(lot.PartId, cancellationToken);
            partCache[lot.PartId] = part;
        }

        if (!warehouseCache.TryGetValue(lot.WarehouseId, out var warehouse))
        {
            warehouse = await _warehouseRepository.GetByIdAsync(lot.WarehouseId, cancellationToken);
            warehouseCache[lot.WarehouseId] = warehouse;
        }

        if (!supplierCache.TryGetValue(lot.SupplierId, out var supplier))
        {
            supplier = await _supplierRepository.GetByIdAsync(lot.SupplierId, cancellationToken);
            supplierCache[lot.SupplierId] = supplier;
        }

        // Load unit info
        Unit? unit = null;
        if (lot.UnitId.HasValue)
        {
            unit = await _unitRepository.GetByIdAsync(lot.UnitId.Value, cancellationToken);
        }

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
            QuantityReceivedInBaseUnit = lot.QuantityReceivedInBaseUnit,
            QuantityAvailable = lot.QuantityAvailable,
            QuantityAvailableInBaseUnit = lot.QuantityAvailableInBaseUnit,
            UnitId = lot.UnitId,
            UnitName = unit?.Name,
            UnitCode = unit?.Symbol,
            BaseUnitName = part?.BaseUnit?.Name,
            BaseUnitCode = part?.BaseUnit?.Symbol,
            CostPrice = lot.CostPrice,
            Currency = lot.Currency,
            TotalCost = lot.GetTotalCost(),
            AvailableCost = lot.GetAvailableCost(),
            HasWarranty = lot.HasWarranty,
            WarrantyPeriodMonths = lot.WarrantyPeriodMonths,
            WarrantyType = lot.WarrantyType,
            WarrantyTerms = lot.WarrantyTerms,
            ReceivingDate = lot.ReceivingDate,
            ExpiryDate = lot.ExpiryDate,
            IsExpired = lot.IsExpired,
            ManufacturerLotNumber = lot.ManufacturerLotNumber,
            Notes = lot.Notes,
            IsActive = lot.IsActive,
            CreatedAt = lot.CreatedDate
        };
    }
}
