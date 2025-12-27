# eQuantic.Core.DomainEvents

[![NuGet](https://img.shields.io/nuget/v/eQuantic.Core.DomainEvents.svg)](https://www.nuget.org/packages/eQuantic.Core.DomainEvents/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/eQuantic.Core.DomainEvents.svg)](https://www.nuget.org/packages/eQuantic.Core.DomainEvents/)
[![Build Status](https://github.com/eQuantic/core-domainevents/workflows/CI%2FCD%20Pipeline/badge.svg)](https://github.com/eQuantic/core-domainevents/actions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.1%20%7C%20net6.0%20%7C%20net8.0%20%7C%20net10.0-blue.svg)](https://github.com/eQuantic/core-domainevents)

Domain Events implementation for DDD (Domain-Driven Design) in .NET.

## 📦 Installation

```bash
dotnet add package eQuantic.Core.DomainEvents
```

## 🚀 Features

| Component                | Description                                         |
| ------------------------ | --------------------------------------------------- |
| `IDomainEvent`           | Base interface for domain events (extends `IEvent`) |
| `DomainEventBase`        | Base class with automatic EventId and OccurredAt    |
| `IAggregateRoot<TKey>`   | Interface for aggregate roots                       |
| `AggregateRoot<TKey>`    | Base class with domain event support                |
| `IDomainEventHandler<T>` | Handler interface for domain events                 |
| `IDomainEventDispatcher` | Dispatcher for domain events                        |

## 📖 Usage

### 1. Define Domain Events

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

### 2. Create Aggregate Roots

```csharp
public class Order : AggregateRoot
{
    public override Guid Id { get; protected set; } = Guid.NewGuid();
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

### 3. Handle Domain Events

```csharp
public class OrderPlacedHandler : IDomainEventHandler<OrderPlacedEvent>
{
    private readonly IEmailService _emailService;

    public OrderPlacedHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct)
    {
        await _emailService.SendOrderConfirmation(@event.OrderId);
    }
}
```

### 4. Register Services

```csharp
services.AddDomainEvents();
services.AddDomainEventHandler<OrderPlacedEvent, OrderPlacedHandler>();
```

### 5. Dispatch Events

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly DbContext _context;
    private readonly IDomainEventDispatcher _dispatcher;

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        // Dispatch domain events after persistence
        await _dispatcher.DispatchEventsAsync(order, ct);
    }
}
```

## 🔗 Ecosystem Integration

This package is part of the eQuantic ecosystem:

| Package                  | Relationship             |
| ------------------------ | ------------------------ |
| `eQuantic.Core.Eventing` | `IDomainEvent : IEvent`  |
| `eQuantic.Core.CQS`      | `INotification : IEvent` |
| `eQuantic.Core`          | Core utilities           |

Both `IDomainEvent` and `INotification` extend the same `IEvent` interface, enabling unified event handling across your application.

## 🎯 Target Frameworks

- .NET Standard 2.1
- .NET 6.0
- .NET 8.0
- .NET 10.0

## 📄 License

MIT License - See [LICENSE](LICENSE) for details.

---

**eQuantic Tech** - Building the future, one component at a time.
