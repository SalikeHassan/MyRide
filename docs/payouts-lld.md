# Payouts Bounded Context — Low Level Design

## Purpose
Supporting domain. Handles paying the driver after the rider has been successfully charged.
Triggered by the SAGA after RiderCharged event. If payout fails, SAGA compensates by refunding the rider.

---

## Ubiquitous Language

| Term          | Meaning                                             |
|---------------|-----------------------------------------------------|
| Recipient     | The driver receiving the payout (maps to Driver)    |
| Sender        | The platform sending the money                      |
| Disbursement  | The actual payout transfer (maps to Transaction)    |

---

## Project Structure

```
Payouts/
├── Payouts.Domain/
│   ├── Aggregates/
│   │   ├── PayoutAggregate.cs
│   │   └── PayoutStatus.cs
│   ├── Commands/
│   │   ├── PayDriverCommand.cs
│   │   └── CancelPayoutCommand.cs
│   ├── Events/
│   │   ├── DriverPaid.cs
│   │   └── DriverPayFailed.cs
│   └── ValueObjects/
│       └── Disbursement.cs
├── Payouts.Application/
│   ├── Ports/
│   │   ├── IPayoutEventStore.cs
│   │   └── IPayoutEventPublisher.cs
│   └── Handlers/
│       ├── PayDriverHandler.cs
│       └── CancelPayoutHandler.cs
└── Payouts.Infrastructure/
    ├── Acl/
    │   └── PaymentsToPayoutsAdapter.cs
    ├── Persistence/
    │   └── MongoPayoutEventStore.cs
    └── Messaging/
        └── PayoutEventPublisher.cs
```

---

## Aggregate — PayoutAggregate

### State
| Property      | Type          | Description                                  |
|---------------|---------------|----------------------------------------------|
| Id            | Guid          | Unique payout identifier (same as PaymentId) |
| TenantId      | string        | Tenant this payout belongs to                |
| RecipientId   | Guid          | Driver receiving the payout                  |
| Disbursement  | Disbursement  | Value object — amount + currency             |
| Status        | PayoutStatus  | Enum: Pending, Paid, Failed, Cancelled       |
| Version       | int           | Inherited from AggregateRoot                 |

### PayoutStatus Enum
```
Pending
Paid
Failed
Cancelled
```

### Invariants
| Scenario                              | Result                          |
|---------------------------------------|---------------------------------|
| PayDriver on already Paid payout      | Throw InvalidOperationException |
| PayDriver on a Cancelled payout       | Throw InvalidOperationException |
| CancelPayout on a Paid payout         | Throw InvalidOperationException |
| CancelPayout on already Cancelled     | Throw InvalidOperationException |
| PayDriver with zero or negative amount| Throw ArgumentException         |

---

## Commands

### PayDriverCommand
| Property      | Type    |
|---------------|---------|
| PayoutId      | Guid    |
| TenantId      | string  |
| RecipientId   | Guid    |
| Amount        | decimal |
| Currency      | string  |
| SimulateFailure | bool  |

### CancelPayoutCommand
| Property  | Type   |
|-----------|--------|
| PayoutId  | Guid   |
| TenantId  | string |
| Reason    | string |

---

## Domain Events

All events implement `IDomainEvent` from SharedKernel (Id, OccurredOn, TenantId).

### DriverPaid
| Property    | Type    |
|-------------|---------|
| PayoutId    | Guid    |
| RecipientId | Guid    |
| Amount      | decimal |
| Currency    | string  |

### DriverPayFailed
| Property    | Type   |
|-------------|--------|
| PayoutId    | Guid   |
| RecipientId | Guid   |
| Reason      | string |

---

## Value Objects

### Disbursement
- Properties: `Amount (decimal)`, `Currency (string)`
- Invariant: Amount must be greater than zero
- Equality: by Amount + Currency

---

## Application Handlers

### PayDriverHandler
1. Create new `PayoutAggregate` via factory method `Pay()`
2. Happy path: raise `DriverPaid` event → SAGA completes
3. Failure path: raise `DriverPayFailed` event → SAGA compensates (RefundRider)
4. Persist events to MongoDB event store
5. Publish event to Service Bus → SAGA picks up

### CancelPayoutHandler
1. Load `PayoutAggregate` by replaying events from MongoDB
2. Call `Cancel(reason)` on aggregate (enforces invariants)
3. Raise `DriverPayFailed` event
4. Persist new event to MongoDB
5. Publish to Service Bus

---

## ACL — PaymentsToPayoutsAdapter (in Payouts.Infrastructure)

Consumer owns the translation. Payouts does NOT know about Payments types.

### Translation map
| Payments language | Payouts language  |
|-------------------|-------------------|
| Transaction       | Disbursement      |
| RiderCharged      | PayDriverCommand  |
| Payer/Payee       | Sender/Recipient  |

### What the adapter does
- Receives `RiderCharged` event (from Service Bus via SAGA)
- Translates it into a `PayDriverCommand`
- Hands it to `PayDriverHandler`

---

## Infrastructure

### MongoPayoutEventStore
- Collection: `{tenantId}_payouts_events`
- Append-only — never update or delete
- Each document: `{ AggregateId, EventType, EventData (JSON), Version, OccurredOn }`
- Load by: filter on `AggregateId` + `TenantId`, order by `Version` ascending

### PayoutEventPublisher
- Publishes to Azure Service Bus topic: `payouts`
- Subscribers: SAGA (listens for `DriverPaid` and `DriverPayFailed`)
- Message includes: event type + full event payload as JSON

---

## Event Flow

```
SAGA sends PayDriverCommand
    → PayDriverHandler
        → PayoutAggregate.Pay()
            ├── Happy path:  DriverPaid raised → SAGA completes
            └── Failure path: DriverPayFailed raised → SAGA sends RefundRider to Payments

SAGA sends CancelPayoutCommand
    → CancelPayoutHandler
        → MongoPayoutEventStore.Load() → replay → PayoutAggregate rebuilt
        → PayoutAggregate.Cancel()
            → DriverPayFailed raised
        → MongoPayoutEventStore.Append()
        → PayoutEventPublisher.Publish()
```

---

## API Endpoints (in MyRide.API)

| Method | Route                          | Handler            |
|--------|--------------------------------|--------------------|
| POST   | /api/payouts/pay               | PayDriverHandler   |
| POST   | /api/payouts/{id}/cancel       | CancelPayoutHandler|

---

## Dependencies
- SharedKernel (AggregateRoot, ValueObject, IDomainEvent)
- MongoDB.Driver (Infrastructure only)
- Azure.Messaging.ServiceBus (Infrastructure only)
- Payments.Domain (Infrastructure/ACL only — for RiderCharged event type)
