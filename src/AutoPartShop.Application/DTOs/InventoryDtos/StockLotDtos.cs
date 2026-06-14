using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.DTOs.InventoryDtos;

public class CreateStockLotRequest
{
    public string LotNumber { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityReceivedInBaseUnit { get; set; }
    public decimal CostPrice { get; set; }
    public decimal CostPriceInBaseUnit { get; set; }
    public Guid? UnitId { get; set; }
    public DateTime ReceivingDate { get; set; }
    public string ManufacturerLotNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string Currency { get; set; } = "USD";
    public string Notes { get; set; } = string.Empty;
    public bool HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
}

public class UpdateStockLotRequest
{
    public DateTime? ExpiryDate { get; set; }
    public string ManufacturerLotNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool? HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
}

public class StockLotResponse
{
    public Guid Id { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSKU { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public string? VariantSku { get; set; }
    // "Base - Variant" composed for display (falls back to the base part name).
    public string DisplayName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public int QuantityReceivedInBaseUnit { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityAvailableInBaseUnit { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? UnitCode { get; set; }
    public string? BaseUnitName { get; set; }
    public string? BaseUnitCode { get; set; }
    public decimal CostPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal AvailableCost { get; set; }
    public bool HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
    public DateTime ReceivingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public string ManufacturerLotNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StockLotPriceHistoryResponse
{
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSKU { get; set; } = string.Empty;
    public List<StockLotHistoryItem> Lots { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal LatestPrice { get; set; }
    public PaginationMeta Pagination { get; set; } = new();
}

public class StockLotHistoryItem
{
    public Guid LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public int QuantityReceivedInBaseUnit { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityAvailableInBaseUnit { get; set; }
    public decimal CostPrice { get; set; }
    public decimal CostPriceInBaseUnit { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? UnitSymbol { get; set; }
    public bool HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public DateTime ReceivingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
}

/// <summary>
/// Returned by GET /api/stocklot/fifo-info/{partId}/{warehouseId}
/// Describes the oldest available lot — used by the frontend to pre-fill
/// selling price and warranty when creating a sale (FIFO pricing).
/// </summary>
public class FifoLotInfoResponse
{
    public bool HasAvailableLot { get; set; }
    public Guid? LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public bool HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
    public int QuantityAvailable { get; set; }
    public DateTime ReceivingDate { get; set; }
}
