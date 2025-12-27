namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a batch/lot of inventory received from a supplier
/// Tracks cost price per lot for accurate inventory costing and price history
/// </summary>
public class StockLot : AuditableEntity
{
    public string LotNumber { get; private set; } = string.Empty;  // Unique lot identifier
    public Guid PartId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid GoodsReceiptLineId { get; private set; }  // Reference to goods receipt that created this lot
    public int QuantityReceived { get; private set; } = 0;  // Total quantity in this lot
    public int QuantityAvailable { get; private set; } = 0;  // Quantity not yet sold/removed
    public decimal CostPrice { get; private set; } = 0;  // Unit cost from supplier
    public string Currency { get; private set; } = "USD";
    public DateTime ReceivingDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }  // Optional expiry date for perishables
    public string ManufacturerLotNumber { get; private set; } = string.Empty;  // Manufacturer's lot/batch number
    public string Notes { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public Part? Part { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<StockLotMovement> Movements { get; set; } = new List<StockLotMovement>();

    private StockLot() { }

    public static StockLot Create(string lotNumber, Guid partId, Guid warehouseId, Guid supplierId,
        Guid goodsReceiptLineId, int quantityReceived, decimal costPrice, DateTime receivingDate,
        string manufacturerLotNumber = "", DateTime? expiryDate = null, string currency = "USD", string notes = "")
    {
        if (string.IsNullOrWhiteSpace(lotNumber))
            throw new ArgumentException("LotNumber cannot be empty", nameof(lotNumber));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty", nameof(warehouseId));

        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        if (goodsReceiptLineId == Guid.Empty)
            throw new ArgumentException("GoodsReceiptLineId cannot be empty", nameof(goodsReceiptLineId));

        if (quantityReceived <= 0)
            throw new ArgumentException("QuantityReceived must be greater than 0", nameof(quantityReceived));

        if (costPrice < 0)
            throw new ArgumentException("CostPrice cannot be negative", nameof(costPrice));

        if (expiryDate.HasValue && expiryDate.Value < receivingDate.Date)
            throw new ArgumentException("ExpiryDate cannot be before ReceivingDate", nameof(expiryDate));

        return new StockLot
        {
            LotNumber = lotNumber.Trim().ToUpper(),
            PartId = partId,
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            GoodsReceiptLineId = goodsReceiptLineId,
            QuantityReceived = quantityReceived,
            QuantityAvailable = quantityReceived,
            CostPrice = costPrice,
            Currency = currency.Trim().ToUpper(),
            ReceivingDate = receivingDate,
            ExpiryDate = expiryDate,
            ManufacturerLotNumber = manufacturerLotNumber?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            IsActive = true
        };
    }

    public void RemoveStock(int quantity, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > QuantityAvailable)
            throw new InvalidOperationException($"Cannot remove {quantity} units. Only {QuantityAvailable} available in lot {LotNumber}");

        QuantityAvailable -= quantity;
    }

    public void AddStock(int quantity, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        QuantityAvailable += quantity;
        if (QuantityAvailable > QuantityReceived)
            QuantityAvailable = QuantityReceived;
    }

    public decimal GetTotalCost() => QuantityReceived * CostPrice;
    public decimal GetAvailableCost() => QuantityAvailable * CostPrice;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow.Date;
    public bool IsEmpty => QuantityAvailable <= 0;

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
