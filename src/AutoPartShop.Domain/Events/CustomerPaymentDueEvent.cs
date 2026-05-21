namespace AutoPartShop.Domain.Events;

public sealed record CustomerPaymentDueEvent(
    Guid    CustomerId,
    string  CustomerName,
    string  CustomerPhone,
    string  CustomerEmail,
    decimal AmountDue,
    string  Currency,
    string  InvoiceNumber,
    DateTime DueDate
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
