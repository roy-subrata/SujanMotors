namespace AutoPartShop.Application.DTOs.InventoryDtos;

public class CreateStockLotRequest
{
    public string LotNumber { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public int QuantityReceived { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime ReceivingDate { get; set; }
    public string ManufacturerLotNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string Currency { get; set; } = "USD";
    public string Notes { get; set; } = string.Empty;
}

public class UpdateStockLotRequest
{
    public DateTime? ExpiryDate { get; set; }
    public string ManufacturerLotNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class StockLotResponse
{
    public Guid Id { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSKU { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public int QuantityAvailable { get; set; }
    public decimal CostPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal AvailableCost { get; set; }
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
}

public class StockLotHistoryItem
{
    public Guid LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public int QuantityAvailable { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime ReceivingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
}
