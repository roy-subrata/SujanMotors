namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Delivery Challan — proof of delivery document generated when goods leave the warehouse.
/// Only used in the "later delivery" flow: SO is Confirmed → ReadyForDelivery → Challan issued → Delivered.
/// For direct handovers the SO goes straight from Confirmed to Delivered; no challan is needed.
/// </summary>
public class Challan : AuditableEntity
{
    public string ChallanNumber    { get; private set; } = string.Empty;
    public Guid   SalesOrderId     { get; private set; }
    public Guid?  InvoiceId        { get; private set; }
    public string Status           { get; private set; } = "DRAFT"; // DRAFT → ISSUED → DELIVERED
    public DateTime? IssuedAt      { get; private set; }
    public DateTime? DeliveredAt   { get; private set; }
    public string DeliveryAddress  { get; private set; } = string.Empty;
    public string ReceiverName     { get; private set; } = string.Empty;
    public string ReceiverPhone    { get; private set; } = string.Empty;
    public string Notes            { get; private set; } = string.Empty;

    // Transport / carrier details
    public string TransportCompany { get; private set; } = string.Empty;
    public string VehicleNumber    { get; private set; } = string.Empty;
    public string DriverName       { get; private set; } = string.Empty;
    public string DriverPhone      { get; private set; } = string.Empty;

    // Navigation
    public SalesOrder? SalesOrder  { get; set; }
    public Invoice?    Invoice     { get; set; }
    public ICollection<ChallanLine> Lines { get; set; } = new List<ChallanLine>();

    private Challan() { }

    public static Challan Create(
        string challanNumber,
        Guid salesOrderId,
        string deliveryAddress   = "",
        string receiverName      = "",
        string receiverPhone     = "",
        string notes             = "",
        string transportCompany  = "",
        string vehicleNumber     = "",
        string driverName        = "",
        string driverPhone       = "")
    {
        if (string.IsNullOrWhiteSpace(challanNumber))
            throw new ArgumentException("ChallanNumber cannot be empty", nameof(challanNumber));
        if (salesOrderId == Guid.Empty)
            throw new ArgumentException("SalesOrderId cannot be empty", nameof(salesOrderId));

        return new Challan
        {
            ChallanNumber   = challanNumber.Trim().ToUpper(),
            SalesOrderId    = salesOrderId,
            DeliveryAddress = deliveryAddress?.Trim()  ?? string.Empty,
            ReceiverName    = receiverName?.Trim()     ?? string.Empty,
            ReceiverPhone   = receiverPhone?.Trim()    ?? string.Empty,
            Notes           = notes?.Trim()            ?? string.Empty,
            TransportCompany = transportCompany?.Trim() ?? string.Empty,
            VehicleNumber   = vehicleNumber?.Trim()    ?? string.Empty,
            DriverName      = driverName?.Trim()       ?? string.Empty,
            DriverPhone     = driverPhone?.Trim()      ?? string.Empty
        };
    }

    public void UpdateTransport(string transportCompany, string vehicleNumber, string driverName, string driverPhone)
    {
        TransportCompany = transportCompany?.Trim() ?? TransportCompany;
        VehicleNumber    = vehicleNumber?.Trim()    ?? VehicleNumber;
        DriverName       = driverName?.Trim()       ?? DriverName;
        DriverPhone      = driverPhone?.Trim()      ?? DriverPhone;
    }

    /// <summary>Issue the challan — makes it ready to travel with the goods.</summary>
    public void Issue()
    {
        if (Status != "DRAFT")
            throw new InvalidOperationException($"Only Draft challans can be issued. Current: {Status}");

        Status   = "ISSUED";
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark as delivered — goods received by customer.
    /// Caller is responsible for also transitioning the SalesOrder to DELIVERED.
    /// </summary>
    public void MarkDelivered(string? receiverName = null, string? receiverPhone = null)
    {
        if (Status != "ISSUED")
            throw new InvalidOperationException($"Only Issued challans can be marked as Delivered. Current: {Status}");

        Status      = "DELIVERED";
        DeliveredAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(receiverName))  ReceiverName  = receiverName.Trim();
        if (!string.IsNullOrWhiteSpace(receiverPhone)) ReceiverPhone = receiverPhone.Trim();
    }

    public void LinkToInvoice(Guid invoiceId) => InvoiceId = invoiceId;

    public void UpdateDeliveryDetails(string address, string receiverName, string receiverPhone)
    {
        DeliveryAddress = address?.Trim()       ?? DeliveryAddress;
        ReceiverName    = receiverName?.Trim()   ?? ReceiverName;
        ReceiverPhone   = receiverPhone?.Trim()  ?? ReceiverPhone;
    }
}

/// <summary>Single line item on a delivery challan.</summary>
public class ChallanLine : AuditableEntity
{
    public Guid    ChallanId          { get; private set; }
    public Guid    PartId             { get; private set; }
    public Guid?   ProductVariantId   { get; private set; }
    public int     Quantity           { get; private set; }
    public string  PartName           { get; private set; } = string.Empty;
    public string  PartSku            { get; private set; } = string.Empty;
    public string  DisplayName        { get; private set; } = string.Empty;
    public string  UnitName           { get; private set; } = string.Empty;
    public int     LineNumber         { get; private set; }

    public Challan? Challan { get; set; }

    private ChallanLine() { }

    public static ChallanLine Create(
        Guid challanId, Guid partId, int quantity,
        string partName, string partSku, string displayName, string unitName,
        int lineNumber, Guid? productVariantId = null)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be > 0", nameof(quantity));

        return new ChallanLine
        {
            ChallanId        = challanId,
            PartId           = partId,
            ProductVariantId = productVariantId,
            Quantity         = quantity,
            PartName         = partName?.Trim()    ?? string.Empty,
            PartSku          = partSku?.Trim()     ?? string.Empty,
            DisplayName      = displayName?.Trim() ?? string.Empty,
            UnitName         = unitName?.Trim()    ?? string.Empty,
            LineNumber       = lineNumber
        };
    }
}
