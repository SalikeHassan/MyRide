# Payments Bounded Context — Low Level Design

## Purpose
Core domain. Handles charging the rider after a ride is completed and refunding if the payout fails.
Triggered by the SAGA — does NOT listen to Rides events directly.

---

## Ubiquitous Language

| Term          | Meaning                                          |
|---------------|--------------------------------------------------|
| Payer         | The person being charged (maps to Rider in Rides)|
| Payee         | The person receiving payment (maps to Driver)    |
| ChargeAmount  | The monetary amount to charge (maps to Fare)     |
| Transaction   | The payment record (maps to Trip)                |

---

## Project Structure

```
Payments/
├── Payments.Domain/
│   ├── Aggregates/
│   │   ├── PaymentAggregate.cs
│   │   └── PaymentStatus.cs
│   ├── Commands/
│   │   ├── ChargeRiderCommand.cs
│   │   └── RefundRiderCommand.cs
│   ├── Events/
│   │   ├── RiderCharged.cs
│   │   ├── RiderChargeFailed.cs
│   │   └── RiderRefunded.cs
│   └── ValueObjects/
│       ├── ChargeAmount.cs
│       └── TransactionId.cs
├── Payments.Application/
│   ├── Ports/
│   │   ├── IPaymentEventStore.cs
│   │   └── IPaymentEventPublisher.cs
│   └── Handlers/
│       ├── ChargeRiderHandler.cs
│       └── RefundRiderHandler.cs
└── Payments.Infrastructure/
    ├── Acl/
    │   └── RidesToPaymentsAdapter.cs
    ├── Persistence/
    │   └── MongoPaymentEventStore.cs
    └── Messaging/
        └── PaymentEventPublisher.cs
```

---

## Aggregate — PaymentAggregate

### State
| Property        | Type          | Description                                    |
|-----------------|---------------|------------------------------------------------|
| Id              | Guid          | Unique payment identifier (same as RideId)     |
| TenantId        | string        | Tenant this payment belongs to                 |
| PayerId         | Guid          | Who is being charged                           |
| PayeeId         | Guid          | Who will receive the payout                    |
| ChargeAmount    | ChargeAmount  | Value object — amount + currency               |
| Status          | PaymentStatus | Enum: Pending, Charged, ChargeFailed, Refunded |
| Version         | int           | Inherited from AggregateRoot                   |

### PaymentStatus Enum
```
Pending
Charged
ChargeFailed
Refunded
```

### Invariants
| Scenario                                  | Result                          |
|-------------------------------------------|---------------------------------|
| ChargeRider on already Charged payment    | Throw InvalidOperationException |
| ChargeRider on a ChargeFailed payment     | Throw InvalidOperationException |
| RefundRider on a Pending payment          | Throw InvalidOperationException |
| RefundRider on a ChargeFailed payment     | Throw InvalidOperationException |
| RefundRider on already Refunded payment   | Throw InvalidOperationException |
| ChargeRider with zero or negative amount  | Throw ArgumentException         |

---

## Commands

### ChargeRiderCommand
| Property      | Type    |
|---------------|---------|
| PaymentId     | Guid    |
| TenantId      | string  |
| PayerId       | Guid    |
| PayeeId       | Guid    |
| Amount        | decimal |
| Currency      | string  |

### RefundRiderCommand
| Property      | Type    |
|---------------|---------|
| PaymentId     | Guid    |
| TenantId      | string  |

---

## Domain Events

All events implement `IDomainEvent` from SharedKernel (Id, OccurredOn, TenantId).

### RiderCharged
| Property      | Type    |
|---------------|---------|
| PaymentId     | Guid    |
| PayerId       | Guid    |
| PayeeId       | Guid    |
| Amount        | decimal |
| Currency      | string  |

### RiderChargeFailed
| Property      | Type    |
|---------------|---------|
| PaymentId     | Guid    |
| PayerId       | Guid    |
| Reason        | string  |

### RiderRefunded
| Property      | Type    |
|---------------|---------|
| PaymentId     | Guid    |
| PayerId       | Guid    |
| Amount        | decimal |
| Currency      | string  |

---

## Value Objects

### ChargeAmount
- Properties: `Amount (decimal)`, `Currency (string)`
- Invariant: Amount must be greater than zero
- Equality: by Amount + Currency

### TransactionId
- Properties: `Value (Guid)`
- Wraps the payment Guid as a strongly typed identifier
- Equality: by Value

---

## Application Handlers

### ChargeRiderHandler
1. Create new `PaymentAggregate` via factory method `Charge()`
2. Raise `RiderCharged` event (simulate — always succeeds for happy path)
3. Persist events to MongoDB event store
4. Publish `RiderCharged` to Service Bus → SAGA picks this up to trigger PayDriver
> Demo toggle: if "simulate failure" flag is set, raise `RiderChargeFailed` instead → SAGA ends

### RefundRiderHandler
1. Load `PaymentAggregate` by replaying events from MongoDB
2. Call `Refund()` on aggregate (enforces invariants)
3. Raise `RiderRefunded` event
4. Persist new event to MongoDB
5. Publish `RiderRefunded` to Service Bus → SAGA marks as rolled back

---

## ACL — RidesToPaymentsAdapter (in Payments.Infrastructure)

Consumer owns the translation. Payments does NOT know about Rides types.

### Translation map
| Rides language  | Payments language  |
|-----------------|--------------------|
| Rider           | Payer              |
| Driver          | Payee              |
| Fare            | ChargeAmount       |
| Trip            | Transaction        |
| RideCompleted   | ChargeRiderCommand |

### What the adapter does
- Receives `RideCompleted` event (from Service Bus message)
- Translates it into a `ChargeRiderCommand`
- Hands it to `ChargeRiderHandler`

---

## Infrastructure

### MongoPaymentEventStore
- Collection: `{tenantId}_payments_events`
- Append-only — never update or delete
- Each document: `{ AggregateId, EventType, EventData (JSON), Version, OccurredOn }`
- Load by: filter on `AggregateId` + `TenantId`, order by `Version` ascending

### PaymentEventPublisher
- Publishes to Azure Service Bus topic: `payments`
- Subscribers: SAGA (listens for `RiderCharged` and `RiderChargeFailed`)
- Message includes: event type + full event payload as JSON

---

## Event Flow

```
SAGA sends ChargeRiderCommand
    → ChargeRiderHandler
        → PaymentAggregate.Charge()
            ├── Happy path:  RiderCharged raised
            └── Failure path: RiderChargeFailed raised
        → MongoPaymentEventStore.Append()
        → PaymentEventPublisher.Publish()
            → SAGA picks up RiderCharged → triggers PayDriver
            → SAGA picks up RiderChargeFailed → ends (nothing to compensate)

SAGA sends RefundRiderCommand (compensation)
    → RefundRiderHandler
        → MongoPaymentEventStore.Load() → replay → PaymentAggregate rebuilt
        → PaymentAggregate.Refund()
            → RiderRefunded raised
        → MongoPaymentEventStore.Append()
        → PaymentEventPublisher.Publish(RiderRefunded)
            → SAGA marks as rolled back
```

---

## API Endpoints (in MyRide.API)

| Method | Route                             | Handler              |
|--------|-----------------------------------|----------------------|
| POST   | /api/payments/charge              | ChargeRiderHandler   |
| POST   | /api/payments/{id}/refund         | RefundRiderHandler   |

---

## Dependencies
- SharedKernel (AggregateRoot, ValueObject, IDomainEvent)
- MongoDB.Driver (Infrastructure only)
- Azure.Messaging.ServiceBus (Infrastructure only)
- Rides.Domain (Infrastructure/ACL only — for RideCompleted event type)
