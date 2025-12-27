namespace AutoPartShop.Application.DTOs.InventoryDtos;

public class CreateStockLotMovementRequest
{
    public Guid StockLotId { get; set; }
    public int Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;  // RECEIPT, SALE, ADJUSTMENT, DAMAGE, RETURN, TRANSFER
    public Guid? ReferenceId { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public DateTime? MovementDate { get; set; }
    public decimal CostAtMovement { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class StockLotMovementResponse
{
    public Guid Id { get; set; }
    public Guid StockLotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public decimal CostAtMovement { get; set; }
    public decimal MovementCost { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StockLotMovementHistoryResponse
{
    public Guid StockLotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public int QuantityAvailable { get; set; }
    public decimal CostPrice { get; set; }
    public List<StockLotMovementItem> Movements { get; set; } = new();
    public int TotalQuantityMoved { get; set; }
    public decimal TotalMovementCost { get; set; }
}

public class StockLotMovementItem
{
    public Guid MovementId { get; set; }
    public int Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public decimal CostAtMovement { get; set; }
    public decimal MovementCost { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class StockLotSummaryResponse
{
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSKU { get; set; } = string.Empty;
    public List<LotSummaryItem> Lots { get; set; } = new();
    public int TotalQuantityOnHand { get; set; }
    public int TotalQuantityAvailable { get; set; }
    public decimal TotalInventoryCost { get; set; }
}

public class LotSummaryItem
{
    public Guid LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityAvailable { get; set; }
    public decimal CostPrice { get; set; }
    public decimal LotCost { get; set; }
    public DateTime ReceivingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public int UnitsSold { get; set; }
}
