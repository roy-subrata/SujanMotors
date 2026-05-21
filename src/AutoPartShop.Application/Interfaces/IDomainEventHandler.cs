using AutoPartShop.Domain.Events;

namespace AutoPartShop.Application.Interfaces;

public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken = default);
}
