using AutoPartShop.Domain.Events;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a sales order to a customer
/// </summary>
public class SalesOrder : AggregateRoot
{
    /// <summary>Optimistic-concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public string SONumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }  // For future customer module
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public Guid? TechnicianId { get; private set; }  // Optional: Technician who recommended the parts
    public string? TechnicianName { get; private set; }  // Technician name for easy reference
    public Guid? CustomerVehicleId { get; private set; }  // Optional: customer's vehicle this purchase is for
    public string VehicleLabel { get; private set; } = string.Empty;  // Denormalized vehicle label for display
    public Guid? WarehouseId { get; private set; }  // Dispatch warehouse
    public string Status { get; private set; } = "PENDING";
    // Lifecycle: PENDING → CONFIRMED → DELIVERED  (direct handover, invoice only)
    //        or: PENDING → CONFIRMED → READY_FOR_DELIVERY → DELIVERED  (later delivery, invoice + challan)
    // Legacy statuses retained for backward compat: DRAFT, PAID, PACKED, SHIPPED, PARTIALLY_SHIPPED, COMPLETED, RETURNED
    public DateTime? PaidDate { get; private set; }
    public DateTime? PackedDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public DateTime SODate { get; private set; }
    public DateTime? ConfirmedDate { get; private set; }
    public DateTime? DeliveryDate { get; private set; }
    public decimal SubTotal { get; private set; } = 0;
    public decimal DiscountPercentage { get; private set; } = 0;
    public decimal DiscountAmount { get; private set; } = 0;
    public decimal TotalAmount { get; private set; } = 0;
    public decimal TaxAmount { get; private set; } = 0;
    public decimal GrandTotal => TotalAmount + TaxAmount;
    public string PaymentStatus { get; private set; } = "PENDING";  // PENDING, PARTIAL, PAID
    public decimal PaidAmount { get; private set; } = 0;
    public string DeliveryAddress { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "BDT";  // ISO 4217 currency code
    public string Channel { get; private set; } = "POS";  // POS | ECOMMERCE | MOBILE | API

    // Navigation properties
    public Customer? Customer { get; set; }
    public Technician? Technician { get; set; }
    public CustomerVehicle? CustomerVehicle { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<SalesOrderLine> LineItems { get; set; } = new List<SalesOrderLine>();
    public Invoice? Invoice { get; set; }

    private SalesOrder() { }

    public static readonly string[] ValidChannels = ["POS", "ECOMMERCE", "MOBILE", "API"];

    public static SalesOrder Create(string soNumber, Guid customerId, string customerName,
        string customerEmail, string customerPhone, Guid? warehouseId = null,
        Guid? technicianId = null, string? technicianName = null,
        string deliveryAddress = "", string notes = "", string currency = "BDT",
        string channel = "POS", Guid? customerVehicleId = null, string vehicleLabel = "")
    {
        if (string.IsNullOrWhiteSpace(soNumber))
            throw new ArgumentException("SONumber cannot be empty", nameof(soNumber));

        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("CustomerName cannot be empty", nameof(customerName));

        var normalizedChannel = string.IsNullOrWhiteSpace(channel) ? "POS" : channel.Trim().ToUpper();
        if (!ValidChannels.Contains(normalizedChannel))
            throw new ArgumentException($"Channel must be one of: {string.Join(", ", ValidChannels)}", nameof(channel));

        return new SalesOrder
        {
            SONumber = soNumber.Trim().ToUpper(),
            CustomerId = customerId,
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail?.Trim() ?? string.Empty,
            CustomerPhone = customerPhone?.Trim() ?? string.Empty,
            TechnicianId = technicianId,
            TechnicianName = technicianName?.Trim(),
            CustomerVehicleId = customerVehicleId,
            VehicleLabel = vehicleLabel?.Trim() ?? string.Empty,
            WarehouseId = warehouseId,
            SODate = DateTime.UtcNow,
            DeliveryAddress = deliveryAddress?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper(),
            Channel = normalizedChannel,
            Status = "PENDING"
        };
    }

    public void Confirm()
    {
        // Accept PENDING (new) and DRAFT (legacy) as the pre-confirm state
        if (Status is not ("PENDING" or "DRAFT"))
            throw new InvalidOperationException($"Only Pending orders can be confirmed. Current: {Status}");

        if (!LineItems.Any())
            throw new InvalidOperationException("SO must have at least one line item");

        if (LineItems.Any(l => l.Quantity <= 0))
            throw new InvalidOperationException("All line items must have a quantity greater than 0");

        Status = "CONFIRMED";
        ConfirmedDate = DateTime.UtcNow;

        RaiseEvent(new SaleOrderConfirmedEvent(
            Id, SONumber, CustomerName, CustomerEmail, CustomerPhone,
            GrandTotal, Currency, Channel, CreatedBy ?? string.Empty));
    }

    /// <summary>
    /// Later-delivery flow only: marks the order as packed/ready to dispatch.
    /// A challan should be generated after this step.
    /// </summary>
    public void MarkAsReadyForDelivery()
    {
        if (Status != "CONFIRMED")
            throw new InvalidOperationException($"Order must be Confirmed before marking Ready For Delivery. Current: {Status}");

        Status = "READY_FOR_DELIVERY";
    }

    public void MarkAsPaid()
    {
        if (Status != "CONFIRMED")
            throw new InvalidOperationException($"Order must be CONFIRMED before marking as PAID. Current: {Status}");

        Status = "PAID";
        PaidDate = DateTime.UtcNow;
    }

    public void MarkAsPacked()
    {
        if (Status != "PAID")
            throw new InvalidOperationException($"Order must be PAID before marking as PACKED. Current: {Status}");

        Status = "PACKED";
        PackedDate = DateTime.UtcNow;
    }

    public void MarkAsPartiallyShipped()
    {
        if (Status is not ("PAID" or "PACKED" or "PARTIALLY_SHIPPED"))
            throw new InvalidOperationException($"Order must be PAID or PACKED before shipping. Current: {Status}");

        Status = "PARTIALLY_SHIPPED";
    }

    public void MarkAsShipped()
    {
        if (Status is not ("PAID" or "PACKED" or "PARTIALLY_SHIPPED"))
            throw new InvalidOperationException($"Order must be PAID or PACKED before marking as SHIPPED. Current: {Status}");

        Status = "SHIPPED";
    }

    public void MarkAsDelivered(DateTime? deliveryDate = null)
    {
        // Direct handover: CONFIRMED → DELIVERED (no challan needed)
        // Later delivery:  READY_FOR_DELIVERY → DELIVERED (challan already issued)
        // Legacy paths:    SHIPPED / PARTIALLY_SHIPPED → DELIVERED
        var allowed = new[] { "CONFIRMED", "READY_FOR_DELIVERY", "SHIPPED", "PARTIALLY_SHIPPED" };
        if (!allowed.Contains(Status))
            throw new InvalidOperationException($"Cannot mark as Delivered from status: {Status}");

        Status = "DELIVERED";
        DeliveryDate = deliveryDate ?? DateTime.UtcNow;
    }

    public void MarkAsCompleted()
    {
        if (Status != "DELIVERED")
            throw new InvalidOperationException($"Order must be DELIVERED before marking as COMPLETED. Current: {Status}");

        Status = "COMPLETED";
        CompletedDate = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is "DELIVERED" or "CANCELLED" or "RETURNED")
            throw new InvalidOperationException($"Cannot cancel a {Status} order");

        Status = "CANCELLED";
    }

    public void MarkAsReturned()
    {
        if (Status is not ("DELIVERED" or "COMPLETED"))
            throw new InvalidOperationException($"Only DELIVERED or COMPLETED orders can be returned. Current: {Status}");

        Status = "RETURNED";
    }

    public void CalculateTotal()
    {
        SubTotal = LineItems.Sum(l => l.TotalPrice);

        if (DiscountPercentage < 0)
            DiscountPercentage = 0;

        if (DiscountPercentage > 100)
            DiscountPercentage = 100;

        DiscountAmount = SubTotal * (DiscountPercentage / 100);

        TotalAmount = SubTotal - DiscountAmount;
        if (TotalAmount < 0)
            TotalAmount = 0;
    }

    public void SetTax(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new ArgumentException("Tax amount cannot be negative", nameof(taxAmount));

        TaxAmount = taxAmount;
    }

    public void SetDiscountPercentage(decimal discountPercentage)
    {
        if (discountPercentage < 0)
            throw new ArgumentException("Discount percentage cannot be negative", nameof(discountPercentage));

        if (discountPercentage > 100)
            throw new ArgumentException("Discount percentage cannot exceed 100%", nameof(discountPercentage));

        DiscountPercentage = discountPercentage;
    }

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than 0", nameof(amount));

        if (amount > GrandTotal - PaidAmount)
            throw new InvalidOperationException("Payment exceeds outstanding amount");

        PaidAmount += amount;
        PaymentStatus = PaidAmount >= GrandTotal ? "PAID" : "PARTIAL";
    }

    /// <summary>
    /// Process a refund by reducing the paid amount
    /// </summary>
    public void ProcessRefund(decimal refundAmount)
    {
        if (refundAmount <= 0)
            throw new ArgumentException("Refund amount must be greater than 0", nameof(refundAmount));

        if (refundAmount > PaidAmount)
            throw new InvalidOperationException("Refund amount cannot exceed paid amount");

        PaidAmount -= refundAmount;
        PaymentStatus = PaidAmount >= GrandTotal ? "PAID" : (PaidAmount > 0 ? "PARTIAL" : "PENDING");
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void UpdateCustomer(Guid customerId, string customerName, string customerEmail, string customerPhone, string customerCity)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("CustomerName cannot be empty", nameof(customerName));

        CustomerId = customerId;
        CustomerName = customerName.Trim();
        CustomerEmail = customerEmail?.Trim() ?? string.Empty;
        CustomerPhone = customerPhone?.Trim() ?? string.Empty;
        DeliveryAddress = customerCity?.Trim() ?? string.Empty;
    }

    public void UpdateDeliveryDate(DateTime? deliveryDate)
    {
        DeliveryDate = deliveryDate;
    }

    public void UpdateCurrency(string currency)
    {
        if (!string.IsNullOrWhiteSpace(currency))
        {
            Currency = currency.Trim().ToUpper();
        }
    }

    public void SetTechnician(Guid? technicianId, string? technicianName)
    {
        TechnicianId = technicianId;
        TechnicianName = technicianName?.Trim();
    }

    public void SetVehicle(Guid? customerVehicleId, string vehicleLabel)
    {
        CustomerVehicleId = customerVehicleId;
        VehicleLabel = vehicleLabel?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Applies a fixed-amount salesperson discount on top of any existing percentage discount.
    /// Must be called after CalculateTotal().
    /// </summary>
    public void ApplyAdditionalDiscount(decimal fixedDiscountAmount)
    {
        if (fixedDiscountAmount < 0)
            throw new ArgumentException("Discount cannot be negative", nameof(fixedDiscountAmount));

        if (fixedDiscountAmount > TotalAmount)
            throw new ArgumentException($"Fixed discount ({fixedDiscountAmount}) cannot exceed order total ({TotalAmount})", nameof(fixedDiscountAmount));

        DiscountAmount += fixedDiscountAmount;
        TotalAmount -= fixedDiscountAmount;
        if (TotalAmount < 0) TotalAmount = 0;
    }

    public void ClearLineItems()
    {
        LineItems.Clear();
    }
}
