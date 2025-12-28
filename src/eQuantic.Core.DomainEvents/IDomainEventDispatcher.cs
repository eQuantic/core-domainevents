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
/// Optionally publishes events to external message brokers via IExternalEventPublisher.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IEventDispatcher _innerDispatcher;
    private readonly IExternalEventPublisher? _externalPublisher;

    /// <summary>
    /// Creates a new DomainEventDispatcher with local dispatch only.
    /// </summary>
    /// <param name="innerDispatcher">The inner event dispatcher for local handlers.</param>
    public DomainEventDispatcher(IEventDispatcher innerDispatcher)
        : this(innerDispatcher, null)
    {
    }

    /// <summary>
    /// Creates a new DomainEventDispatcher with optional external publishing.
    /// </summary>
    /// <param name="innerDispatcher">The inner event dispatcher for local handlers.</param>
    /// <param name="externalPublisher">Optional external event publisher (Azure, AWS, RabbitMQ, etc.).</param>
    public DomainEventDispatcher(
        IEventDispatcher innerDispatcher,
        IExternalEventPublisher? externalPublisher)
    {
        _innerDispatcher = innerDispatcher;
        _externalPublisher = externalPublisher;
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
        
        if (!events.Any())
        {
            return;
        }

        // Dispatch to local handlers
        await _innerDispatcher.DispatchAsync(events, cancellationToken);
        
        // Publish to external message broker (if configured)
        if (_externalPublisher != null)
        {
            await _externalPublisher.PublishAsync(events, cancellationToken);
        }
        
        aggregateRoot.ClearUncommittedEvents();
    }
}
