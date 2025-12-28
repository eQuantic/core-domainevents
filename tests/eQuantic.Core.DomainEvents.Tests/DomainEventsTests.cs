using eQuantic.Core.DomainEvents.Extensions;
using eQuantic.Core.Eventing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eQuantic.Core.DomainEvents.Tests;

// ============================================================
// TEST DOMAIN EVENTS & AGGREGATES
// ============================================================

public class OrderPlacedEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public decimal Amount { get; }

    public OrderPlacedEvent(Guid orderId, decimal amount)
    {
        OrderId = orderId;
        Amount = amount;
    }
}

public class OrderCancelledEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string Reason { get; }

    public OrderCancelledEvent(Guid orderId, string reason)
    {
        OrderId = orderId;
        Reason = reason;
    }
}

public class Order : AggregateRoot
{
    private Guid _id = Guid.NewGuid();
    public override Guid Id { get => _id; protected set => _id = value; }
    
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }

    public void Place(decimal amount)
    {
        TotalAmount = amount;
        RaiseDomainEvent(new OrderPlacedEvent(Id, amount));
    }

    public void Cancel(string reason)
    {
        IsCancelled = true;
        RaiseDomainEvent(new OrderCancelledEvent(Id, reason));
    }
}

public class OrderPlacedHandler : IDomainEventHandler<OrderPlacedEvent>
{
    public static List<OrderPlacedEvent> HandledEvents { get; } = new();

    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        HandledEvents.Add(@event);
        return Task.CompletedTask;
    }
}

public class OrderCancelledHandler : IDomainEventHandler<OrderCancelledEvent>
{
    public static List<OrderCancelledEvent> HandledEvents { get; } = new();

    public Task HandleAsync(OrderCancelledEvent @event, CancellationToken cancellationToken = default)
    {
        HandledEvents.Add(@event);
        return Task.CompletedTask;
    }
}

// ============================================================
// DOMAIN EVENT TESTS
// ============================================================

public class DomainEventBaseTests
{
    [Fact]
    public void DomainEvent_ShouldHaveUniqueEventId()
    {
        // Arrange & Act
        var event1 = new OrderPlacedEvent(Guid.NewGuid(), 100m);
        var event2 = new OrderPlacedEvent(Guid.NewGuid(), 200m);

        // Assert
        event1.EventId.Should().NotBeEmpty();
        event2.EventId.Should().NotBeEmpty();
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void DomainEvent_ShouldHaveOccurredAtCloseToNow()
    {
        // Arrange & Act
        var @event = new OrderPlacedEvent(Guid.NewGuid(), 100m);

        // Assert
        @event.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DomainEvent_ShouldImplementIEvent()
    {
        // Arrange & Act
        var @event = new OrderPlacedEvent(Guid.NewGuid(), 100m);

        // Assert
        @event.Should().BeAssignableTo<IEvent>();
        @event.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void DomainEvent_ShouldStoreCustomProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;

        // Act
        var @event = new OrderPlacedEvent(orderId, amount);

        // Assert
        @event.OrderId.Should().Be(orderId);
        @event.Amount.Should().Be(amount);
    }
}

// ============================================================
// AGGREGATE ROOT TESTS
// ============================================================

public class AggregateRootTests
{
    [Fact]
    public void AggregateRoot_ShouldHaveId()
    {
        // Arrange & Act
        var order = new Order();

        // Assert
        order.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AggregateRoot_RaiseDomainEvent_ShouldAddToUncommittedEvents()
    {
        // Arrange
        var order = new Order();

        // Act
        order.Place(100m);

        // Assert
        order.DomainEvents.Should().HaveCount(1);
        order.DomainEvents.First().Should().BeOfType<OrderPlacedEvent>();
    }

    [Fact]
    public void AggregateRoot_MultipleEvents_ShouldPreserveOrder()
    {
        // Arrange
        var order = new Order();

        // Act
        order.Place(100m);
        order.Cancel("Customer request");

        // Assert
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.First().Should().BeOfType<OrderPlacedEvent>();
        order.DomainEvents.Last().Should().BeOfType<OrderCancelledEvent>();
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var order = new Order();
        order.Place(100m);
        order.Cancel("Test");
        order.DomainEvents.Should().HaveCount(2);

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AggregateRoot_DomainEvents_ShouldBeReadOnly()
    {
        // Arrange
        var order = new Order();
        order.Place(100m);

        // Act
        var events = order.DomainEvents;

        // Assert
        events.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void AggregateRoot_ShouldImplementIAggregateRoot()
    {
        // Arrange & Act
        var order = new Order();

        // Assert
        order.Should().BeAssignableTo<IAggregateRoot>();
        order.Should().BeAssignableTo<IAggregateRoot<Guid>>();
        order.Should().BeAssignableTo<IEventSource>();
    }

    [Fact]
    public void AggregateRoot_GetUncommittedEvents_ShouldReturnEvents()
    {
        // Arrange
        var order = new Order();
        order.Place(100m);

        // Act
        var events = order.GetUncommittedEvents();

        // Assert
        events.Should().HaveCount(1);
        events.First().Should().BeOfType<OrderPlacedEvent>();
    }
}

// ============================================================
// DOMAIN EVENT DISPATCHER TESTS
// ============================================================

public class DomainEventDispatcherTests : IDisposable
{
    public DomainEventDispatcherTests()
    {
        OrderPlacedHandler.HandledEvents.Clear();
        OrderCancelledHandler.HandledEvents.Clear();
    }

    public void Dispose()
    {
        OrderPlacedHandler.HandledEvents.Clear();
        OrderCancelledHandler.HandledEvents.Clear();
    }

    [Fact]
    public async Task DispatchEventsAsync_ShouldDispatchAndClearEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDomainEvents();
        services.AddDomainEventHandler<OrderPlacedEvent, OrderPlacedHandler>();
        
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        
        var order = new Order();
        order.Place(99.99m);
        
        // Act
        await dispatcher.DispatchEventsAsync(order);

        // Assert
        OrderPlacedHandler.HandledEvents.Should().HaveCount(1);
        OrderPlacedHandler.HandledEvents.First().Amount.Should().Be(99.99m);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithMultipleEvents_ShouldDispatchAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDomainEvents();
        services.AddDomainEventHandler<OrderPlacedEvent, OrderPlacedHandler>();
        services.AddDomainEventHandler<OrderCancelledEvent, OrderCancelledHandler>();
        
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        
        var order = new Order();
        order.Place(100m);
        order.Cancel("Test cancellation");
        
        // Act
        await dispatcher.DispatchEventsAsync(order);

        // Assert
        OrderPlacedHandler.HandledEvents.Should().HaveCount(1);
        OrderCancelledHandler.HandledEvents.Should().HaveCount(1);
        OrderCancelledHandler.HandledEvents.First().Reason.Should().Be("Test cancellation");
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithNoEvents_ShouldNotFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDomainEvents();
        
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        
        var order = new Order();
        
        // Act
        var act = () => dispatcher.DispatchEventsAsync(order);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_SingleEvent_ShouldDispatch()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDomainEvents();
        services.AddDomainEventHandler<OrderPlacedEvent, OrderPlacedHandler>();
        
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        
        var @event = new OrderPlacedEvent(Guid.NewGuid(), 50m);
        
        // Act
        await dispatcher.DispatchAsync(@event);

        // Assert
        OrderPlacedHandler.HandledEvents.Should().HaveCount(1);
        OrderPlacedHandler.HandledEvents.First().Amount.Should().Be(50m);
    }
}

// ============================================================
// SERVICE COLLECTION EXTENSIONS TESTS
// ============================================================

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDomainEvents_ShouldRegisterDispatcher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDomainEvents();
        var provider = services.BuildServiceProvider();

        // Assert
        var dispatcher = provider.GetService<IDomainEventDispatcher>();
        dispatcher.Should().NotBeNull();
        dispatcher.Should().BeOfType<DomainEventDispatcher>();
    }

    [Fact]
    public void AddDomainEvents_ShouldRegisterEventDispatcher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDomainEvents();
        var provider = services.BuildServiceProvider();

        // Assert
        var dispatcher = provider.GetService<IEventDispatcher>();
        dispatcher.Should().NotBeNull();
    }

    [Fact]
    public void AddDomainEventHandler_ShouldRegisterHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDomainEvents();

        // Act
        services.AddDomainEventHandler<OrderPlacedEvent, OrderPlacedHandler>();
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IDomainEventHandler<OrderPlacedEvent>>();
        handler.Should().NotBeNull();
        handler.Should().BeOfType<OrderPlacedHandler>();
    }

    [Fact]
    public void AddDomainEventHandler_ShouldAlsoRegisterAsIEventHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDomainEvents();

        // Act
        services.AddDomainEventHandler<OrderPlacedEvent, OrderPlacedHandler>();
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IEventHandler<OrderPlacedEvent>>();
        handler.Should().NotBeNull();
        handler.Should().BeOfType<OrderPlacedHandler>();
    }

    [Fact]
    public void AddDomainEvents_ShouldReturnDomainEventsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddDomainEvents();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<DomainEventsBuilder>();
        result.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void AddDomainEventHandler_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDomainEvents();

        // Act
        var result = services.AddDomainEventHandler<OrderPlacedEvent, OrderPlacedHandler>();

        // Assert
        result.Should().BeSameAs(services);
    }
}

