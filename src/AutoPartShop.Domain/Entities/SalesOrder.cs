namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a sales order to a customer
/// </summary>
public class SalesOrder : AuditableEntity
{
    public string SONumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }  // For future customer module
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public Guid? TechnicianId { get; private set; }  // Optional: Technician who recommended the parts
    public string? TechnicianName { get; private set; }  // Technician name for easy reference
    public Guid? WarehouseId { get; private set; }  // Dispatch warehouse
    public string Status { get; private set; } = "DRAFT";  // DRAFT, CONFIRMED, PARTIALLY_SHIPPED, SHIPPED, DELIVERED, CANCELLED
    public DateTime SODate { get; private set; }
    public DateTime? ConfirmedDate { get; private set; }
    public DateTime? DeliveryDate { get; private set; }
    public decimal TotalAmount { get; private set; } = 0;
    public decimal TaxAmount { get; private set; } = 0;
    public decimal GrandTotal => TotalAmount + TaxAmount;
    public string PaymentStatus { get; private set; } = "PENDING";  // PENDING, PARTIAL, PAID
    public decimal PaidAmount { get; private set; } = 0;
    public string DeliveryAddress { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "BDT";  // ISO 4217 currency code

    // Navigation properties
    public Customer? Customer { get; set; }
    public Technician? Technician { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<SalesOrderLine> LineItems { get; set; } = new List<SalesOrderLine>();
    public Invoice? Invoice { get; set; }

    private SalesOrder() { }

    public static SalesOrder Create(string soNumber, Guid customerId, string customerName,
        string customerEmail, string customerPhone, Guid? warehouseId = null,
        Guid? technicianId = null, string? technicianName = null,
        string deliveryAddress = "", string notes = "", string currency = "BDT")
    {
        if (string.IsNullOrWhiteSpace(soNumber))
            throw new ArgumentException("SONumber cannot be empty", nameof(soNumber));

        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("CustomerName cannot be empty", nameof(customerName));

        return new SalesOrder
        {
            SONumber = soNumber.Trim().ToUpper(),
            CustomerId = customerId,
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail?.Trim() ?? string.Empty,
            CustomerPhone = customerPhone?.Trim() ?? string.Empty,
            TechnicianId = technicianId,
            TechnicianName = technicianName?.Trim(),
            WarehouseId = warehouseId,
            SODate = DateTime.UtcNow,
            DeliveryAddress = deliveryAddress?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper(),
            Status = "DRAFT"
        };
    }

    public void Confirm()
    {
        if (Status != "DRAFT")
            throw new InvalidOperationException("Only draft SOs can be confirmed");

        if (!LineItems.Any())
            throw new InvalidOperationException("SO must have at least one line item");

        Status = "CONFIRMED";
        ConfirmedDate = DateTime.UtcNow;
    }

    public void MarkAsPartiallyShipped()
    {
        Status = "PARTIALLY_SHIPPED";
    }

    public void MarkAsShipped()
    {
        Status = "SHIPPED";
    }

    public void MarkAsDelivered(DateTime? deliveryDate = null)
    {
        Status = "DELIVERED";
        DeliveryDate = deliveryDate ?? DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is "SHIPPED" or "DELIVERED" or "CANCELLED")
            throw new InvalidOperationException($"Cannot cancel a {Status} SO");

        Status = "CANCELLED";
    }

    public void CalculateTotal()
    {
        TotalAmount = LineItems.Sum(l => l.TotalPrice);
    }

    public void SetTax(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new ArgumentException("Tax amount cannot be negative", nameof(taxAmount));

        TaxAmount = taxAmount;
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

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void SetTechnician(Guid? technicianId, string? technicianName)
    {
        TechnicianId = technicianId;
        TechnicianName = technicianName?.Trim();
    }
}
