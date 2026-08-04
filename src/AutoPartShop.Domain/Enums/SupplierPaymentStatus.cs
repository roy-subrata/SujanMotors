namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.SupplierPayment"/>.
/// </summary>
public enum SupplierPaymentStatus
{
    PENDING,
    PROCESSING,
    COMPLETED,
    FAILED,
    CANCELLED,
    RETURNED
}
