namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a batch/lot of inventory received from a supplier.
/// Tracks cost price AND selling price per lot — different lots of the same part
/// can have different selling prices and warranty terms (FIFO-based pricing).
/// </summary>
public class StockLot : AuditableEntity
{
    public string LotNumber { get; private set; } = string.Empty;  // Unique lot identifier
    public Guid PartId { get; private set; }
    public Guid? VariantId { get; private set; }  // null = part-level lot; set = variant-scoped (SKU-level) lot
    public Guid WarehouseId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid GoodsReceiptLineId { get; private set; }  // Reference to goods receipt that created this lot
    public Guid? UnitId { get; private set; }  // Unit in which lot quantities are measured
    public int QuantityReceived { get; private set; } = 0;  // Total quantity in this lot
    public int QuantityReceivedInBaseUnit { get; private set; } = 0;  // Quantity in base unit
    public int QuantityAvailable { get; private set; } = 0;  // Quantity not yet sold/removed
    public int QuantityAvailableInBaseUnit { get; private set; } = 0;  // Available in base unit
    public decimal CostPrice { get; private set; } = 0;  // Unit cost from supplier
    public decimal CostPriceInBaseUnit { get; private set; } = 0;  // Cost per base unit
    // Lot-level selling price (source of truth for FIFO pricing — overrides Part.SellingPrice for this lot)
    public decimal SellingPrice { get; private set; } = 0;
    public string Currency { get; private set; } = "USD";
    public DateTime ReceivingDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }  // Optional expiry date for perishables
    public string ManufacturerLotNumber { get; private set; } = string.Empty;  // Manufacturer's lot/batch number
    public string Notes { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    // Inventory status (GRN spec): AVAILABLE = sellable; DAMAGED / QUARANTINE = held, excluded from sale.
    public string Status { get; private set; } = "AVAILABLE";

    // Lot-level warranty (some lots may not have warranty even if the part normally does)
    public bool HasWarranty { get; private set; } = false;
    public int? WarrantyPeriodMonths { get; private set; }
    public string? WarrantyType { get; private set; }
    public string? WarrantyTerms { get; private set; }

    // Navigation properties
    public Product? Part { get; set; }
    public ProductVariant? Variant { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Supplier? Supplier { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<StockLotMovement> Movements { get; set; } = new List<StockLotMovement>();

    private StockLot() { }

    public static StockLot Create(string lotNumber, Guid partId, Guid warehouseId, Guid supplierId,
        Guid goodsReceiptLineId, int quantityReceived, decimal costPrice, DateTime receivingDate,
        string manufacturerLotNumber = "", DateTime? expiryDate = null, string currency = "USD",
        string notes = "", Guid? unitId = null, int quantityReceivedInBaseUnit = 0,
        decimal costPriceInBaseUnit = 0, decimal sellingPrice = 0,
        bool hasWarranty = false, int? warrantyPeriodMonths = null,
        string? warrantyType = null, string? warrantyTerms = null, Guid? variantId = null,
        string status = "AVAILABLE")
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

        if (sellingPrice < 0)
            throw new ArgumentException("SellingPrice cannot be negative", nameof(sellingPrice));

        if (expiryDate.HasValue && expiryDate.Value < receivingDate.Date)
            throw new ArgumentException("ExpiryDate cannot be before ReceivingDate", nameof(expiryDate));

        return new StockLot
        {
            LotNumber = lotNumber.Trim().ToUpper(),
            PartId = partId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            GoodsReceiptLineId = goodsReceiptLineId,
            QuantityReceived = quantityReceived,
            QuantityReceivedInBaseUnit = quantityReceivedInBaseUnit > 0 ? quantityReceivedInBaseUnit : quantityReceived,
            QuantityAvailable = quantityReceived,
            QuantityAvailableInBaseUnit = quantityReceivedInBaseUnit > 0 ? quantityReceivedInBaseUnit : quantityReceived,
            CostPrice = costPrice,
            CostPriceInBaseUnit = costPriceInBaseUnit > 0 ? costPriceInBaseUnit : costPrice,
            SellingPrice = sellingPrice,
            HasWarranty = hasWarranty,
            WarrantyPeriodMonths = warrantyPeriodMonths,
            WarrantyType = warrantyType?.Trim(),
            WarrantyTerms = warrantyTerms?.Trim(),
            Currency = currency.Trim().ToUpper(),
            ReceivingDate = receivingDate,
            ExpiryDate = expiryDate,
            ManufacturerLotNumber = manufacturerLotNumber?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            UnitId = unitId,
            IsActive = true,
            Status = string.IsNullOrWhiteSpace(status) ? "AVAILABLE" : status.Trim().ToUpper()
        };
    }

    public void RemoveStock(int quantity, int quantityInBaseUnit = 0, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > QuantityAvailable)
            throw new InvalidOperationException($"Cannot remove {quantity} units. Only {QuantityAvailable} available in lot {LotNumber}");

        QuantityAvailable -= quantity;
        QuantityAvailableInBaseUnit -= quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity;
    }

    public void AddStock(int quantity, int quantityInBaseUnit = 0, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        QuantityAvailable += quantity;
        QuantityAvailableInBaseUnit += quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity;
        if (QuantityAvailable > QuantityReceived)
            QuantityAvailable = QuantityReceived;
        if (QuantityAvailableInBaseUnit > QuantityReceivedInBaseUnit)
            QuantityAvailableInBaseUnit = QuantityReceivedInBaseUnit;
    }

    /// <summary>
    /// Increases the received capacity so that AddStock won't be capped.
    /// Used for returns where stock is added back beyond the original lot quantity.
    /// </summary>
    public void IncreaseCapacity(int quantity, int quantityInBaseUnit = 0)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        QuantityReceived += quantity;
        QuantityReceivedInBaseUnit += quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity;
    }

    /// <summary>
    /// Factory method for creating lots from sales returns (no supplier/goods receipt required)
    /// </summary>
    public static StockLot CreateForReturn(string lotNumber, Guid partId, Guid warehouseId,
        int quantityReceived, decimal costPrice, DateTime receivingDate,
        string returnNumber = "", string currency = "USD", string notes = "",
        Guid? unitId = null, int quantityReceivedInBaseUnit = 0, decimal costPriceInBaseUnit = 0,
        decimal sellingPrice = 0, Guid? variantId = null)
    {
        if (string.IsNullOrWhiteSpace(lotNumber))
            throw new ArgumentException("LotNumber cannot be empty", nameof(lotNumber));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty", nameof(warehouseId));

        if (quantityReceived <= 0)
            throw new ArgumentException("QuantityReceived must be greater than 0", nameof(quantityReceived));

        if (costPrice < 0)
            throw new ArgumentException("CostPrice cannot be negative", nameof(costPrice));

        return new StockLot
        {
            LotNumber = lotNumber.Trim().ToUpper(),
            PartId = partId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            SupplierId = Guid.Empty,
            GoodsReceiptLineId = Guid.Empty,
            QuantityReceived = quantityReceived,
            QuantityReceivedInBaseUnit = quantityReceivedInBaseUnit > 0 ? quantityReceivedInBaseUnit : quantityReceived,
            QuantityAvailable = quantityReceived,
            QuantityAvailableInBaseUnit = quantityReceivedInBaseUnit > 0 ? quantityReceivedInBaseUnit : quantityReceived,
            CostPrice = costPrice,
            CostPriceInBaseUnit = costPriceInBaseUnit > 0 ? costPriceInBaseUnit : costPrice,
            SellingPrice = sellingPrice,
            Currency = currency.Trim().ToUpper(),
            ReceivingDate = receivingDate,
            ManufacturerLotNumber = returnNumber?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            UnitId = unitId,
            IsActive = true
        };
    }

    public decimal GetTotalCost() => QuantityReceived * CostPrice;
    public decimal GetTotalCostInBaseUnit() => QuantityReceivedInBaseUnit * CostPriceInBaseUnit;
    public decimal GetAvailableCost() => QuantityAvailable * CostPrice;
    public decimal GetAvailableCostInBaseUnit() => QuantityAvailableInBaseUnit * CostPriceInBaseUnit;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow.Date;
    public bool IsEmpty => QuantityAvailable <= 0;

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void UpdateDetails(string manufacturerLotNumber, DateTime? expiryDate, string notes)
    {
        if (expiryDate.HasValue && expiryDate.Value < ReceivingDate.Date)
            throw new ArgumentException("ExpiryDate cannot be before ReceivingDate", nameof(expiryDate));

        ManufacturerLotNumber = manufacturerLotNumber?.Trim() ?? string.Empty;
        ExpiryDate = expiryDate;
        UpdateNotes(notes);
    }

    /// <summary>
    /// Updates the lot-level selling price and warranty data.
    /// Called when an operator adjusts per-lot pricing after receipt.
    /// </summary>
    public void UpdatePriceAndWarranty(decimal sellingPrice, bool hasWarranty,
        int? warrantyPeriodMonths, string? warrantyType, string? warrantyTerms)
    {
        if (sellingPrice < 0)
            throw new ArgumentException("SellingPrice cannot be negative", nameof(sellingPrice));

        SellingPrice = sellingPrice;
        HasWarranty = hasWarranty;
        WarrantyPeriodMonths = hasWarranty ? warrantyPeriodMonths : null;
        WarrantyType = hasWarranty ? warrantyType?.Trim() : null;
        WarrantyTerms = hasWarranty ? warrantyTerms?.Trim() : null;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
