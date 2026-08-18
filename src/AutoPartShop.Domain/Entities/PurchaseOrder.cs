using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a purchase order from a supplier
/// </summary>
public class PurchaseOrder : AuditableEntity
{
    public string PONumber { get; private set; } = string.Empty;  // Unique PO number
    public Guid SupplierId { get; private set; }
    public Guid? WarehouseId { get; private set; }  // Expected delivery warehouse
    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.DRAFT;
    public DateTime PODate { get; private set; }
    public DateTime ExpectedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }
    public decimal SubTotal { get; private set; } = 0;
    public decimal TaxAmount { get; private set; } = 0;
    public decimal TaxPercentage { get; private set; } = 0;
    public decimal DiscountAmount { get; private set; } = 0;  // Computed final discount applied to the total
    public decimal DiscountPercentage { get; private set; } = 0;
    public decimal DiscountFixedAmount { get; private set; } = 0;  // User-entered fixed discount amount (optional)
    public string DiscountType { get; private set; } = "TOTAL";  // BULK or TOTAL
    public decimal TotalAmount { get; private set; } = 0;
    public PurchaseOrderPaymentStatus PaymentStatus { get; private set; } = PurchaseOrderPaymentStatus.PENDING;
    public decimal PaidAmount { get; private set; } = 0;
    public decimal CreditAppliedAmount { get; private set; } = 0;  // Total credit notes applied to this PO

    /// <summary>
    /// What is still payable: the order total less cash paid AND credit notes applied. A credit
    /// note settles part of the order just as a payment does, so leaving it out reported the full
    /// balance as still owing after a note had been consumed — the note was recorded and delivered
    /// nothing.
    /// </summary>
    public decimal OutstandingAmount => TotalAmount - PaidAmount - CreditAppliedAmount;
    public string Notes { get; private set; } = string.Empty;
    public string ApprovedBy { get; private set; } = string.Empty;
    public DateTime? ApprovedDate { get; private set; }
    public string Currency { get; private set; } = "BDT";  // ISO 4217 currency code
    public decimal? BaseTotalAmount { get; private set; }  // TotalAmount converted to base currency at PO time
    public decimal? FxRateToBase { get; private set; }  // Exchange rate applied when BaseTotalAmount was captured (1 = same as base)

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
            Status = PurchaseOrderStatus.DRAFT,
            Notes = notes?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper()
        };
    }

    public void Submit()
    {
        if (Status != PurchaseOrderStatus.DRAFT)
            throw new InvalidOperationException("Only draft POs can be submitted");

        if (!LineItems.Any())
            throw new InvalidOperationException("PO must have at least one line item");

        Status = PurchaseOrderStatus.SUBMITTED;
    }

    public void Confirm(string approvedBy)
    {
        if (Status != PurchaseOrderStatus.SUBMITTED)
            throw new InvalidOperationException("Only submitted POs can be confirmed");

        Status = PurchaseOrderStatus.CONFIRMED;
        ApprovedBy = approvedBy;
        ApprovedDate = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is PurchaseOrderStatus.DELIVERED or PurchaseOrderStatus.CANCELLED or PurchaseOrderStatus.PARTIAL)
            throw new InvalidOperationException($"Cannot cancel a {Status} PO. A PARTIAL order has accepted goods receipts — create a purchase return first.");

        Status = PurchaseOrderStatus.CANCELLED;
    }

    public void CalculateTotal()
    {
        // Calculate subtotal from line items
        SubTotal = LineItems.Sum(l => l.TotalPrice);

        // Calculate tax amount
        TaxAmount = SubTotal * (TaxPercentage / 100);

        // Calculate discount: the larger of the percentage-derived amount and the fixed amount
        var percentageDiscount = SubTotal * (DiscountPercentage / 100);
        DiscountAmount = Math.Max(percentageDiscount, DiscountFixedAmount);

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

    /// <summary>
    /// Sets the discount inputs (percentage and/or fixed amount). The effective discount is
    /// computed in <see cref="CalculateTotal"/> as the larger of the two.
    /// </summary>
    public void SetDiscount(decimal percentage, decimal fixedAmount, string discountType)
    {
        if (percentage < 0)
            throw new ArgumentException("Discount percentage cannot be negative", nameof(percentage));

        if (percentage > 100)
            throw new ArgumentException("Discount percentage cannot exceed 100%", nameof(percentage));

        if (fixedAmount < 0)
            throw new ArgumentException("Discount amount cannot be negative", nameof(fixedAmount));

        DiscountPercentage = percentage;
        DiscountFixedAmount = fixedAmount;
        DiscountType = string.IsNullOrWhiteSpace(discountType) ? "TOTAL" : discountType.Trim().ToUpper();
    }

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than 0", nameof(amount));

        // Credits already applied settle part of the order, so they reduce what is still payable.
        // Ignoring them here let a PO be paid past its true balance once a credit note was applied.
        if (amount > OutstandingAmount)
            throw new InvalidOperationException("Payment exceeds outstanding amount");

        PaidAmount += amount;
        PaymentStatus = PaidAmount + CreditAppliedAmount >= TotalAmount
            ? PurchaseOrderPaymentStatus.PAID
            : PurchaseOrderPaymentStatus.PARTIAL;
    }

    /// <summary>
    /// Apply credit note amount to this purchase order
    /// </summary>
    public void ApplyCredit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit amount must be greater than 0", nameof(amount));

        if (amount > OutstandingAmount)
            throw new InvalidOperationException("Credit amount exceeds outstanding amount");

        CreditAppliedAmount += amount;

        // Update payment status based on total payments + credits
        var totalApplied = PaidAmount + CreditAppliedAmount;
        PaymentStatus = totalApplied >= TotalAmount ? PurchaseOrderPaymentStatus.PAID : PurchaseOrderPaymentStatus.PARTIAL;
    }

    public void MarkAsDelivered(DateTime? deliveryDate = null)
    {
        if (Status is not (PurchaseOrderStatus.CONFIRMED or PurchaseOrderStatus.PARTIAL))
            throw new InvalidOperationException($"Only CONFIRMED or PARTIAL purchase orders can be marked as delivered. Current: {Status}");

        Status = PurchaseOrderStatus.DELIVERED;
        ActualDeliveryDate = deliveryDate ?? DateTime.UtcNow;
    }

    public void UpdateReceiptStatus()
    {
        // Only update status if PO is CONFIRMED or PARTIAL
        if (Status != PurchaseOrderStatus.CONFIRMED && Status != PurchaseOrderStatus.PARTIAL)
            return;

        // Calculate total ordered quantity across all line items
        var totalOrdered = LineItems.Sum(l => l.Quantity);
        if (totalOrdered == 0)
            return;

        // Line ReceivedQuantity is the source of truth here — it has already been
        // recomputed from accepted GRNs by the caller. Deriving from GoodsReceipts
        // again would miss the GRN being accepted right now (still VERIFIED in this
        // snapshot), leaving the PO stuck at CONFIRMED after its final receipt.
        var totalAccepted = LineItems.Sum(l => l.ReceivedQuantity);

        // Update status based on received quantities
        if (totalAccepted >= totalOrdered)
        {
            Status = PurchaseOrderStatus.DELIVERED;
            ActualDeliveryDate = DateTime.UtcNow;
        }
        else if (totalAccepted > 0)
        {
            Status = PurchaseOrderStatus.PARTIAL;
        }
    }

    /// <summary>
    /// Computes the still-outstanding quantity for a PO line that can be received on a new/edited GRN.
    /// Single source of truth for "remaining to receive":
    ///   outstanding = OrderedQty
    ///     - sum of AcceptedQuantity over ACCEPTED GRN lines           (rejected units are freed)
    ///     - sum of ReceivedQuantity over not-yet-accepted GRN lines   (in-flight PENDING/VERIFIED is reserved)
    /// Pass <paramref name="excludeGoodsReceiptId"/> when editing a GRN so its own lines are not double-counted.
    /// Requires <see cref="GoodsReceipts"/> (and their LineItems) to be loaded.
    /// </summary>
    public int GetOutstandingQuantity(Guid purchaseOrderLineId, Guid? excludeGoodsReceiptId = null)
    {
        var poLine = LineItems.FirstOrDefault(l => l.Id == purchaseOrderLineId);
        if (poLine is null)
            return 0;

        var accepted = GoodsReceipts
            .Where(gr => gr.Status == GoodsReceiptStatus.ACCEPTED)
            .SelectMany(gr => gr.LineItems)
            .Where(grl => grl.PurchaseOrderLineId == purchaseOrderLineId)
            .Sum(grl => grl.AcceptedQuantity);

        var inFlight = GoodsReceipts
            .Where(gr => (gr.Status == GoodsReceiptStatus.PENDING || gr.Status == GoodsReceiptStatus.VERIFIED)
                && gr.Id != (excludeGoodsReceiptId ?? Guid.Empty))
            .SelectMany(gr => gr.LineItems)
            .Where(grl => grl.PurchaseOrderLineId == purchaseOrderLineId)
            .Sum(grl => grl.ReceivedQuantity);

        return poLine.Quantity - accepted - inFlight;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Captures the base-currency equivalent of <see cref="TotalAmount"/> at PO time so reports
    /// stay stable even if exchange rates change later. Call after totals are final.
    /// </summary>
    public void SetFxBaseAmount(decimal baseAmount, decimal rateToBase)
    {
        if (baseAmount < 0)
            throw new ArgumentException("Base amount cannot be negative", nameof(baseAmount));

        if (rateToBase <= 0)
            throw new ArgumentException("Rate to base must be greater than 0", nameof(rateToBase));

        BaseTotalAmount = baseAmount;
        FxRateToBase = rateToBase;
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
        if (Status != PurchaseOrderStatus.DRAFT)
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
        if (Status != PurchaseOrderStatus.DRAFT)
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
                existing.Update(
                    itemData.Quantity,
                    itemData.UnitPrice,
                    itemData.UnitId,
                    itemData.QuantityInBaseUnit,
                    itemData.Description,
                    itemData.VariantId
                );
            }
            else
            {
                var newLine = PurchaseOrderLine.Create(
                    Id,
                    itemData.PartId,
                    itemData.Quantity,
                    itemData.UnitPrice,
                    lineNumber,
                    itemData.UnitId,
                    itemData.QuantityInBaseUnit,
                    itemData.Description,
                    itemData.VariantId
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
    Guid? VariantId,
    int Quantity,
    decimal UnitPrice,
    Guid? UnitId,
    int QuantityInBaseUnit,
    string Description = ""
);
