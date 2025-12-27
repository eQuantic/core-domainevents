using eQuantic.Core.Eventing;
using eQuantic.Core.Eventing.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Core.DomainEvents.Extensions;

/// <summary>
/// Extension methods for registering domain event services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds domain event services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="strategy">The dispatch strategy to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDomainEvents(
        this IServiceCollection services,
        EventDispatchStrategy strategy = EventDispatchStrategy.WhenAll)
    {
        // Add the base event dispatcher
        services.AddEventDispatcher(strategy);
        
        // Add the domain event dispatcher wrapper
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        
        return services;
    }

    /// <summary>
    /// Registers a domain event handler for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type.</typeparam>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        // Register as both IDomainEventHandler and IEventHandler
        services.AddTransient<IDomainEventHandler<TEvent>, THandler>();
        services.AddTransient<IEventHandler<TEvent>, THandler>();
        return services;
    }
}
