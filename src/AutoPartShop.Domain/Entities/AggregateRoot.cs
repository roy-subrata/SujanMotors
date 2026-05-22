using AutoPartShop.Domain.Events;

namespace AutoPartShop.Domain.Entities;

/// <summary>Base for entities that own a transaction boundary and can raise domain events.</summary>
public abstract class AggregateRoot : AuditableEntity
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected void RaiseEvent(IDomainEvent evt) => _events.Add(evt);

    public void ClearEvents() => _events.Clear();
}
