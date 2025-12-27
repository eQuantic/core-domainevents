using eQuantic.Core.Eventing;

namespace eQuantic.Core.DomainEvents;

/// <summary>
/// Handler for a specific domain event type.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<in TEvent> : IEventHandler<TEvent> 
    where TEvent : IDomainEvent
{
}
