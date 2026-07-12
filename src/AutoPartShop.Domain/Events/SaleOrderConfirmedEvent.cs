namespace AutoPartShop.Domain.Events;

public sealed record SaleOrderConfirmedEvent(
    Guid SalesOrderId,
    string SONumber,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    decimal GrandTotal,
    string Currency,
    string Channel,
    string CreatedBy
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
