namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a purchase order from a supplier
/// </summary>
public class PurchaseOrder : AuditableEntity
{
    public string PONumber { get; private set; } = string.Empty;  // Unique PO number
    public Guid SupplierId { get; private set; }
    public Guid? WarehouseId { get; private set; }  // Expected delivery warehouse
    public string Status { get; private set; } = "DRAFT";  // DRAFT, SUBMITTED, CONFIRMED, PARTIAL, DELIVERED, CANCELLED
    public DateTime PODate { get; private set; }
    public DateTime ExpectedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }
    public decimal SubTotal { get; private set; } = 0;
    public decimal TaxAmount { get; private set; } = 0;
    public decimal TaxPercentage { get; private set; } = 0;
    public decimal DiscountAmount { get; private set; } = 0;
    public decimal DiscountPercentage { get; private set; } = 0;
    public decimal TotalAmount { get; private set; } = 0;
    public string PaymentStatus { get; private set; } = "PENDING";  // PENDING, PARTIAL, PAID
    public decimal PaidAmount { get; private set; } = 0;
    public decimal CreditAppliedAmount { get; private set; } = 0;  // Total credit notes applied to this PO
    public string Notes { get; private set; } = string.Empty;
    public string ApprovedBy { get; private set; } = string.Empty;
    public DateTime? ApprovedDate { get; private set; }
    public string Currency { get; private set; } = "BDT";  // ISO 4217 currency code

    // Navigation properties
    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<PurchaseOrderLine> LineItems { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
    public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();

    private PurchaseOrder() { }

    public static PurchaseOrder Create(string poNumber, Guid supplierId, Guid? warehouseId,
        DateTime expectedDeliveryDate, string notes = "", string currency = "BDT")
    {
        if (string.IsNullOrWhiteSpace(poNumber))
            throw new ArgumentException("PONumber cannot be empty", nameof(poNumber));

        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        if (expectedDeliveryDate < DateTime.UtcNow.Date)
            throw new ArgumentException("Expected delivery date cannot be in the past", nameof(expectedDeliveryDate));

        return new PurchaseOrder
        {
            PONumber = poNumber.Trim().ToUpper(),
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            PODate = DateTime.UtcNow,
            ExpectedDeliveryDate = expectedDeliveryDate,
            Status = "DRAFT",
            Notes = notes?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper()
        };
    }

    public void Submit()
    {
        if (Status != "DRAFT")
            throw new InvalidOperationException("Only draft POs can be submitted");

        if (!LineItems.Any())
            throw new InvalidOperationException("PO must have at least one line item");

        Status = "SUBMITTED";
    }

    public void Confirm(string approvedBy)
    {
        if (Status != "SUBMITTED")
            throw new InvalidOperationException("Only submitted POs can be confirmed");

        Status = "CONFIRMED";
        ApprovedBy = approvedBy;
        ApprovedDate = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is "DELIVERED" or "CANCELLED")
            throw new InvalidOperationException($"Cannot cancel a {Status} PO");

        Status = "CANCELLED";
    }

    public void CalculateTotal()
    {
        // Calculate subtotal from line items
        SubTotal = LineItems.Sum(l => l.TotalPrice);

        // Calculate tax amount
        TaxAmount = SubTotal * (TaxPercentage / 100);

        // Calculate discount amount
        DiscountAmount = SubTotal * (DiscountPercentage / 100);

        // Calculate final total: SubTotal + Tax - Discount
        TotalAmount = SubTotal + TaxAmount - DiscountAmount;
    }

    public void SetTaxPercentage(decimal percentage)
    {
        if (percentage < 0)
            throw new ArgumentException("Tax percentage cannot be negative", nameof(percentage));

        TaxPercentage = percentage;
    }

    public void SetDiscountPercentage(decimal percentage)
    {
        if (percentage < 0)
            throw new ArgumentException("Discount percentage cannot be negative", nameof(percentage));

        if (percentage > 100)
            throw new ArgumentException("Discount percentage cannot exceed 100%", nameof(percentage));

        DiscountPercentage = percentage;
    }

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than 0", nameof(amount));

        if (amount > TotalAmount - PaidAmount)
            throw new InvalidOperationException("Payment exceeds outstanding amount");

        PaidAmount += amount;
        PaymentStatus = PaidAmount >= TotalAmount ? "PAID" : "PARTIAL";
    }

    /// <summary>
    /// Apply credit note amount to this purchase order
    /// </summary>
    public void ApplyCredit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit amount must be greater than 0", nameof(amount));

        var outstandingAmount = TotalAmount - PaidAmount;
        if (amount > outstandingAmount)
            throw new InvalidOperationException("Credit amount exceeds outstanding amount");

        CreditAppliedAmount += amount;
        
        // Update payment status based on total payments + credits
        var totalApplied = PaidAmount + CreditAppliedAmount;
        PaymentStatus = totalApplied >= TotalAmount ? "PAID" : "PARTIAL";
    }

    public void MarkAsDelivered(DateTime? deliveryDate = null)
    {
        Status = "DELIVERED";
        ActualDeliveryDate = deliveryDate ?? DateTime.UtcNow;
    }

    public void UpdateReceiptStatus()
    {
        // Only update status if PO is CONFIRMED or PARTIAL
        if (Status != "CONFIRMED" && Status != "PARTIAL")
            return;

        // Calculate total ordered quantity across all line items
        var totalOrdered = LineItems.Sum(l => l.Quantity);
        if (totalOrdered == 0)
            return;

        // Calculate total accepted quantity from all accepted goods receipts
        var totalAccepted = GoodsReceipts
            .Where(gr => gr.Status == "ACCEPTED")
            .SelectMany(gr => gr.LineItems)
            .Sum(l => l.AcceptedQuantity);

        // Update status based on received quantities
        if (totalAccepted >= totalOrdered)
        {
            Status = "DELIVERED";
            ActualDeliveryDate = DateTime.UtcNow;
        }
        else if (totalAccepted > 0)
        {
            Status = "PARTIAL";
        }
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void UpdateExpectedDeliveryDate(DateTime date)
    {
        ExpectedDeliveryDate = date;
    }

    /// <summary>
    /// Clears all line items from the purchase order
    /// </summary>
    public void ClearLineItems()
    {
        LineItems.Clear();
    }

    /// <summary>
    /// Removes a specific line item by ID
    /// </summary>
    public bool RemoveLineItem(Guid lineItemId)
    {
        var lineItem = LineItems.FirstOrDefault(l => l.Id == lineItemId);
        if (lineItem != null)
        {
            return LineItems.Remove(lineItem);
        }
        return false;
    }

    /// <summary>
    /// Gets a line item by ID
    /// </summary>
    public PurchaseOrderLine? GetLineItem(Guid lineItemId)
    {
        return LineItems.FirstOrDefault(l => l.Id == lineItemId);
    }

    /// <summary>
    /// Updates the supplier ID (only allowed in DRAFT status)
    /// </summary>
    public void UpdateSupplier(Guid supplierId)
    {
        if (Status != "DRAFT")
            throw new InvalidOperationException("Can only change supplier on draft POs");

        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        SupplierId = supplierId;
    }

    /// <summary>
    /// Synchronizes line items with the provided data (upsert pattern).
    /// - Items with matching Id are updated
    /// - Items without Id (or Id not found) are added as new
    /// - Existing items not in the input list are removed
    /// </summary>
    public void SyncLineItems(IEnumerable<LineItemData> items)
    {
        if (Status != "DRAFT")
            throw new InvalidOperationException("Can only modify line items on draft POs");

        var itemsList = items.ToList();
        var itemsById = itemsList
            .Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty)
            .ToDictionary(i => i.Id!.Value);

        // Remove items not in the new list
        var toRemove = LineItems
            .Where(l => !itemsById.ContainsKey(l.Id))
            .ToList();

        foreach (var item in toRemove)
        {
            LineItems.Remove(item);
        }

        // Update existing or add new
        int lineNumber = 1;
        foreach (var itemData in itemsList)
        {
            var existing = itemData.Id.HasValue && itemData.Id.Value != Guid.Empty
                ? LineItems.FirstOrDefault(l => l.Id == itemData.Id.Value)
                : null;

            if (existing != null)
            {
                // Update existing line item
                existing.Update(
                    itemData.Quantity,
                    itemData.UnitPrice,
                    itemData.UnitId,
                    itemData.QuantityInBaseUnit,
                    itemData.Description
                );
            }
            else
            {
                // Add new line item
                var newLine = PurchaseOrderLine.Create(
                    Id,
                    itemData.PartId,
                    itemData.Quantity,
                    itemData.UnitPrice,
                    lineNumber,
                    itemData.UnitId,
                    itemData.QuantityInBaseUnit,
                    itemData.Description
                );
                LineItems.Add(newLine);
            }
            lineNumber++;
        }

        // Recalculate totals after sync
        CalculateTotal();
    }
}

/// <summary>
/// Data transfer object for line item synchronization within the domain
/// </summary>
public record LineItemData(
    Guid? Id,
    Guid PartId,
    int Quantity,
    decimal UnitPrice,
    Guid? UnitId,
    int QuantityInBaseUnit,
    string Description = ""
);
