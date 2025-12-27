using eQuantic.Core.Eventing;

namespace eQuantic.Core.DomainEvents;

/// <summary>
/// Dispatcher for domain events to their handlers.
/// </summary>
public interface IDomainEventDispatcher : IEventDispatcher
{
    /// <summary>
    /// Dispatches all uncommitted events from an aggregate root.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root with uncommitted events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchEventsAsync(IAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);
}

/// <summary>
/// Domain event dispatcher implementation that uses the base InMemoryEventDispatcher.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IEventDispatcher _innerDispatcher;

    public DomainEventDispatcher(IEventDispatcher innerDispatcher)
    {
        _innerDispatcher = innerDispatcher;
    }

    /// <inheritdoc />
    public Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IEvent
    {
        return _innerDispatcher.DispatchAsync(@event, cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
    {
        return _innerDispatcher.DispatchAsync(events, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchEventsAsync(IAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
    {
        var events = aggregateRoot.GetUncommittedEvents();
        
        // Use the collection-based dispatch which handles dynamic types correctly
        await _innerDispatcher.DispatchAsync(events, cancellationToken);
        
        aggregateRoot.ClearUncommittedEvents();
    }
}
