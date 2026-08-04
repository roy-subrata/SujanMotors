namespace AutoPartShop.Domain.Enums;

/// <summary>Payment status of a <see cref="Entities.SalesOrder"/>.</summary>
public enum SalesOrderPaymentStatus
{
    PENDING,
    PARTIAL,
    PAID
}
