namespace AutoPartShop.Domain.Enums;

/// <summary>Lifecycle status of a <see cref="Entities.CustomerPayment"/>.</summary>
public enum CustomerPaymentStatus
{
    PENDING,
    PROCESSING,
    COMPLETED,
    FAILED,
    REFUNDED,
    CANCELLED
}
