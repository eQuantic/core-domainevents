using eQuantic.Core.Eventing;

namespace eQuantic.Core.DomainEvents;

/// <summary>
/// Interface for aggregate roots that can raise domain events.
/// Aggregate roots are the entry point to aggregates and are responsible for
/// maintaining consistency and raising domain events.
/// </summary>
/// <typeparam name="TKey">The type of the aggregate identifier.</typeparam>
public interface IAggregateRoot<TKey> : IEventSource
{
    /// <summary>
    /// Gets the unique identifier of the aggregate.
    /// </summary>
    TKey Id { get; }
}

/// <summary>
/// Non-generic interface for aggregate roots.
/// </summary>
public interface IAggregateRoot : IEventSource
{
}
