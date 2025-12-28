using eQuantic.Core.Eventing;
using eQuantic.Core.Eventing.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    /// <returns>A builder for further configuration.</returns>
    public static DomainEventsBuilder AddDomainEvents(
        this IServiceCollection services,
        EventDispatchStrategy strategy = EventDispatchStrategy.WhenAll)
    {
        // Add the base event dispatcher
        services.AddEventDispatcher(strategy);
        
        // Add the domain event dispatcher wrapper
        services.AddSingleton<IDomainEventDispatcher>(sp =>
        {
            var innerDispatcher = sp.GetRequiredService<IEventDispatcher>();
            var externalPublisher = sp.GetService<IExternalEventPublisher>();
            return new DomainEventDispatcher(innerDispatcher, externalPublisher);
        });
        
        return new DomainEventsBuilder(services);
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

/// <summary>
/// Builder for configuring domain events.
/// </summary>
public class DomainEventsBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Creates a new builder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public DomainEventsBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Configures external event publishing using the specified publisher.
    /// </summary>
    /// <typeparam name="TPublisher">The external publisher type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public DomainEventsBuilder UseExternalPublisher<TPublisher>()
        where TPublisher : class, IExternalEventPublisher
    {
        Services.AddSingleton<IExternalEventPublisher, TPublisher>();
        return this;
    }

    /// <summary>
    /// Configures external event subscription using the specified subscriber.
    /// The subscriber will consume events from external message brokers and dispatch them to local handlers.
    /// </summary>
    /// <typeparam name="TSubscriber">The external subscriber type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public DomainEventsBuilder UseExternalSubscriber<TSubscriber>()
        where TSubscriber : class, IExternalEventSubscriber
    {
        Services.AddSingleton<IExternalEventSubscriber, TSubscriber>();
        Services.AddHostedService<DomainEventSubscriberHostedService>();
        return this;
    }

    /// <summary>
    /// Registers a domain event handler.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type.</typeparam>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public DomainEventsBuilder AddHandler<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        Services.AddDomainEventHandler<TEvent, THandler>();
        return this;
    }
}

/// <summary>
/// Hosted service that manages the domain event subscriber lifecycle.
/// </summary>
public class DomainEventSubscriberHostedService : IHostedService
{
    private readonly IExternalEventSubscriber _subscriber;

    public DomainEventSubscriberHostedService(IExternalEventSubscriber subscriber)
    {
        _subscriber = subscriber;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _subscriber.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _subscriber.StopAsync(cancellationToken);
    }
}
