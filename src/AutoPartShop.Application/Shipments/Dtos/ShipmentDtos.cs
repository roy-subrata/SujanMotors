namespace AutoPartShop.Application.Shipments.Dtos;

public class CreateShipmentRequest
{
    public Guid SalesOrderId { get; set; }
    public string? CourierName { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<CreateShipmentLineRequest> Lines { get; set; } = new();
}

public class CreateShipmentLineRequest
{
    public Guid SalesOrderLineId { get; set; }
    public Guid PartId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public int QuantityInBaseUnit { get; set; }
}

public class UpdateTrackingRequest
{
    public string? CourierName { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
}

public class DispatchShipmentRequest
{
    public string? TrackingNumber { get; set; }
    public string? CourierName { get; set; }
}

public class FailShipmentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class ShipmentResponse
{
    public Guid Id { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public string? CourierName { get; set; }
    public string? TrackingNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? DispatchedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public DateTime? FailedDate { get; set; }
    public string? FailureReason { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<ShipmentLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ShipmentLineResponse
{
    public Guid Id { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public Guid? ProductVariantId { get; set; }
    public string? VariantName { get; set; }
    public string? VariantSku { get; set; }
    public int Quantity { get; set; }
    public int QuantityInBaseUnit { get; set; }
}
