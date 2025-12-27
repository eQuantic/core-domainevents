using eQuantic.Core.Eventing;

namespace eQuantic.Core.DomainEvents;

/// <summary>
/// Base class for aggregate roots with built-in domain event support.
/// </summary>
/// <typeparam name="TKey">The type of the aggregate identifier.</typeparam>
public abstract class AggregateRoot<TKey> : EventSourceBase, IAggregateRoot<TKey>, IAggregateRoot
{
    /// <inheritdoc />
    public abstract TKey Id { get; protected set; }

    /// <summary>
    /// Raises a domain event by adding it to the uncommitted events collection.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        AddEvent(domainEvent);
    }

    /// <summary>
    /// Gets all uncommitted domain events.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => 
        GetUncommittedEvents().OfType<IDomainEvent>().ToList().AsReadOnly();

    /// <summary>
    /// Clears all uncommitted domain events.
    /// </summary>
    public void ClearDomainEvents() => ClearUncommittedEvents();
}

/// <summary>
/// Aggregate root with Guid as the default key type.
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
}
