namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.Shipment"/>: PENDING → DISPATCHED → IN_TRANSIT → DELIVERED | FAILED.
/// </summary>
public enum ShipmentStatus
{
    PENDING,
    DISPATCHED,
    IN_TRANSIT,
    DELIVERED,
    FAILED
}
