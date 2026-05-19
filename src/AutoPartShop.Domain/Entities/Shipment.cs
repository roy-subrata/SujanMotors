namespace AutoPartShop.Domain.Entities;

public class Shipment : AuditableEntity
{
    public string ShipmentNumber { get; private set; } = string.Empty;   // SHP-YYYY-XXXXX
    public Guid SalesOrderId { get; private set; }
    public string? CourierName { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string Status { get; private set; } = "PENDING";  // PENDING → DISPATCHED → IN_TRANSIT → DELIVERED | FAILED
    public DateTime? EstimatedDeliveryDate { get; private set; }
    public DateTime? DispatchedDate { get; private set; }
    public DateTime? DeliveredDate { get; private set; }
    public DateTime? FailedDate { get; private set; }
    public string? FailureReason { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    // Navigation
    public SalesOrder? SalesOrder { get; set; }
    public ICollection<ShipmentLine> Lines { get; set; } = new List<ShipmentLine>();

    private Shipment() { }

    public static Shipment Create(
        string shipmentNumber,
        Guid salesOrderId,
        string? courierName = null,
        string? trackingNumber = null,
        DateTime? estimatedDeliveryDate = null,
        string notes = "")
    {
        if (string.IsNullOrWhiteSpace(shipmentNumber))
            throw new ArgumentException("ShipmentNumber cannot be empty", nameof(shipmentNumber));

        if (salesOrderId == Guid.Empty)
            throw new ArgumentException("SalesOrderId cannot be empty", nameof(salesOrderId));

        return new Shipment
        {
            ShipmentNumber = shipmentNumber.Trim().ToUpper(),
            SalesOrderId = salesOrderId,
            CourierName = courierName?.Trim(),
            TrackingNumber = trackingNumber?.Trim(),
            EstimatedDeliveryDate = estimatedDeliveryDate,
            Notes = notes?.Trim() ?? string.Empty,
            Status = "PENDING"
        };
    }

    public void Dispatch(string? trackingNumber = null, string? courierName = null)
    {
        if (Status != "PENDING")
            throw new InvalidOperationException($"Only PENDING shipments can be dispatched. Current: {Status}");

        if (!Lines.Any())
            throw new InvalidOperationException("Shipment must have at least one line before dispatching");

        if (!string.IsNullOrWhiteSpace(trackingNumber))
            TrackingNumber = trackingNumber.Trim();

        if (!string.IsNullOrWhiteSpace(courierName))
            CourierName = courierName.Trim();

        Status = "DISPATCHED";
        DispatchedDate = DateTime.UtcNow;
    }

    public void MarkInTransit()
    {
        if (Status != "DISPATCHED")
            throw new InvalidOperationException($"Shipment must be DISPATCHED before marking IN_TRANSIT. Current: {Status}");

        Status = "IN_TRANSIT";
    }

    public void MarkDelivered()
    {
        if (Status is not ("DISPATCHED" or "IN_TRANSIT"))
            throw new InvalidOperationException($"Shipment must be DISPATCHED or IN_TRANSIT to be delivered. Current: {Status}");

        Status = "DELIVERED";
        DeliveredDate = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        if (Status is "DELIVERED" or "FAILED")
            throw new InvalidOperationException($"Cannot fail a {Status} shipment");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason is required", nameof(reason));

        Status = "FAILED";
        FailedDate = DateTime.UtcNow;
        FailureReason = reason.Trim();
    }

    public void UpdateTracking(string? courierName, string? trackingNumber, DateTime? estimatedDeliveryDate)
    {
        if (!string.IsNullOrWhiteSpace(courierName))
            CourierName = courierName.Trim();

        if (!string.IsNullOrWhiteSpace(trackingNumber))
            TrackingNumber = trackingNumber.Trim();

        if (estimatedDeliveryDate.HasValue)
            EstimatedDeliveryDate = estimatedDeliveryDate;
    }
}
