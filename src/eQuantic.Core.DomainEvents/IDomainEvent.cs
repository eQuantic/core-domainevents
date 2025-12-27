using eQuantic.Core.Eventing;

namespace eQuantic.Core.DomainEvents;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// Extends IEvent from Core.Eventing for ecosystem integration.
/// </summary>
public interface IDomainEvent : IEvent
{
}

/// <summary>
/// Base class for domain events with automatic EventId and OccurredAt.
/// </summary>
public abstract class DomainEventBase : EventBase, IDomainEvent
{
}
