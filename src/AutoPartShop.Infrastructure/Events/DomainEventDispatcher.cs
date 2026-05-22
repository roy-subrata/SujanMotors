using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider services, ILogger<DomainEventDispatcher> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => DispatchAsync([domainEvent], cancellationToken);

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var evt in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());

            foreach (var handler in _services.GetServices(handlerType))
            {
                if (handler is null) continue;
                try
                {
                    await ((dynamic)handler).HandleAsync((dynamic)evt, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler {Handler} failed for {Event}",
                        handler.GetType().Name, evt.GetType().Name);
                }
            }
        }
    }
}
