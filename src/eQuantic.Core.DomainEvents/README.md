# eQuantic.Core.DomainEvents

[![NuGet](https://img.shields.io/nuget/v/eQuantic.Core.DomainEvents.svg)](https://www.nuget.org/packages/eQuantic.Core.DomainEvents/)
[![Build Status](https://github.com/eQuantic/core-domainevents/workflows/CI%2FCD/badge.svg)](https://github.com/eQuantic/core-domainevents/actions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Domain Events implementation for DDD (Domain-Driven Design) in .NET.

## Installation

```bash
dotnet add package eQuantic.Core.DomainEvents
```

## Features

- `IDomainEvent` - Base interface for domain events (extends `IEvent` from Core.Eventing)
- `DomainEventBase` - Base class for domain events with automatic EventId and OccurredAt
- `IAggregateRoot<TKey>` - Interface for aggregate roots with event sourcing
- `AggregateRoot<TKey>` - Base class for aggregate roots with domain event support
- `IDomainEventHandler<T>` - Handler interface for domain events
- `IDomainEventDispatcher` - Dispatcher for domain events

## Usage

### Define a Domain Event

```csharp
public class OrderPlacedEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public decimal TotalAmount { get; }

    public OrderPlacedEvent(Guid orderId, decimal totalAmount)
    {
        OrderId = orderId;
        TotalAmount = totalAmount;
    }
}
```

### Create an Aggregate Root

```csharp
public class Order : AggregateRoot
{
    public override Guid Id { get; protected set; }
    public decimal TotalAmount { get; private set; }
    public bool IsPlaced { get; private set; }

    public void Place(decimal amount)
    {
        TotalAmount = amount;
        IsPlaced = true;

        // Raise domain event
        RaiseDomainEvent(new OrderPlacedEvent(Id, amount));
    }
}
```

### Handle Domain Events

```csharp
public class OrderPlacedHandler : IDomainEventHandler<OrderPlacedEvent>
{
    public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct)
    {
        Console.WriteLine($"Order {@event.OrderId} placed for ${@event.TotalAmount}");
    }
}
```

### Register Services

```csharp
services.AddDomainEvents();
services.AddDomainEventHandler<OrderPlacedEvent, OrderPlacedHandler>();
```

### Dispatch Events

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly IDomainEventDispatcher _dispatcher;

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        // Save to database...

        // Dispatch domain events
        await _dispatcher.DispatchEventsAsync(order, ct);
    }
}
```

## Integration with eQuantic Ecosystem

This package integrates with:

- **eQuantic.Core.Eventing** - Shared `IEvent` interface
- **eQuantic.Core.CQS** - `INotification` also extends `IEvent`

## License

MIT License - See [LICENSE](LICENSE) for details.
