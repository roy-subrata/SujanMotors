namespace AutoPartShop.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
