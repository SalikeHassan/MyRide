# Demo Prep — Todo List
**Target:** CTO demo, Thursday 2026-03-19
**Goal:** Showcase clean DDD / CQRS / Event Sourcing / SAGA patterns end-to-end

---

## Priority Order

```
1. Common libraries + ICommand/IQuery abstractions
2. Projection service (EventStoreDB → SQL read model)
3. HTTP-based publisher — remove NoOp, wire BFF orchestration
4. SAGA orchestrator — RideCompleted → ChargeRider → PayDriver (with compensation)
5. Driver stream — cross-aggregate invariance (if time permits)
```

Items 6 (request/response folder reorganisation) and 7 (full SAGA choreography via Service Bus) are deferred post-demo.

---

## 1. Common Libraries + Abstractions

### New projects to create
| Project | Layer | Purpose |
|---|---|---|
| `Common.Domain` | Domain | `AggregateRoot`, `ValueObject` base classes |
| `Common.Application` | Application | `ICommand`, `IQuery`, `ICommandHandler<T>`, `IQueryHandler<T,R>` |
| `SharedKernel` | Cross-cutting | Keep as-is — `IDomainEvent`, `TenantId` primitives |

### What moves where
- `AggregateRoot` — currently in `SharedKernel` → move to `Common.Domain`
- `IDomainEvent` — stays in `SharedKernel`
- `ICommand` / `IQuery` — new, goes in `Common.Application`
- `ICommandHandler<TCommand>` — new, goes in `Common.Application`
- `IQueryHandler<TQuery, TResult>` — new, goes in `Common.Application`

### Dependency rule
```
Common.Application  →  Common.Domain  →  SharedKernel
```
No project references should go the other way.

### ICommand / IQuery pattern
All command and query classes must implement the marker interface:

```csharp
// Common.Application
public interface ICommand { }
public interface IQuery<TResult> { }

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command);
}

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query);
}
```

All existing handlers (`StartRideHandler`, `AcceptRideHandler`, `CompleteRideHandler`, etc.) to implement `ICommandHandler<T>` or `IQueryHandler<T, R>`.

---

## 2. Projection Service

### Purpose
Replace the dual-write in handlers (append event + upsert SQL in the same handler) with an EventStoreDB subscription that updates the SQL read model asynchronously.

### What changes in handlers
Remove `rideReadStore.Upsert(...)` calls from all ride handlers. Handlers become pure:
```
Load aggregate → apply command → append event → done
```

### Projection service design
- `RideProjectionService : BackgroundService` in `Rides.Infrastructure`
- Subscribe to EventStoreDB `$all` stream filtered to ride event types
- On each event: deserialise → upsert `RideReadModels` in SQL

### Events to handle
| Event | Action on read model |
|---|---|
| `RideStarted` | Insert new `RideReadModel` with status `Requested` |
| `RideAccepted` | Update status → `InProgress` |
| `RideCompleted` | Update status → `Completed` |
| `RideCancelled` | Update status → `Cancelled` |

### Checkpoint
- In-memory for demo (acceptable — on restart it will re-read from the beginning)
- Production path: persist checkpoint position to SQL

### Registration
`RideProjectionService` registered as a hosted service in `Rides.API/Program.cs`.

---

## 3. HTTP-Based Publisher — Remove NoOp

### Current state
- `Rides.API` wired to `NoOpRideEventPublisher` — publishes nothing
- `Payments.API` and `Payouts.API` have real `ServiceBusPublisher` registered but Service Bus emulator not running

### What to do
- **Remove** `NoOpRideEventPublisher`
- **Remove** `RideEventPublisher` (Service Bus), `PaymentEventPublisher`, `PayoutEventPublisher` — Service Bus publishers no longer needed
- **Remove** `IPaymentEventPublisher` and `IPayoutEventPublisher` ports — these are no longer called in handlers
- **Remove** `eventPublisher.Publish(...)` calls from all handlers in Payments and Payouts
- The BFF becomes the sole orchestrator — downstream events are triggered by BFF, not by the services themselves

### Why this is clean
The services (Rides, Payments, Payouts) become pure command executors. They do one thing each. The BFF orchestrates the sequence. When Service Bus is added later, the BFF wires Service Bus publishing — not the individual services.

---

## 4. SAGA Orchestrator

### Context
The SAGA lives in `MyRide.API` (the BFF). It is triggered when `CompleteRide` is called. The BFF is the **orchestrator** — it calls each downstream service in sequence and handles compensation if any step fails.

The failure simulator (already in the UI) drives the compensation path — the CTO can watch a payment fail and see the SAGA trigger a refund in real time.

### SAGA flow — happy path
```
POST /rides/{id}/complete
  │
  ├─ Step 1: Rides.API  →  CompleteRide          ✅
  │
  ├─ Step 2: Payments.API  →  ChargeRider         ✅  (returns PaymentId)
  │
  └─ Step 3: Payouts.API  →  PayDriver            ✅
                                                   SAGA complete ✅
```

### SAGA flow — PayDriver fails (compensation)
```
POST /rides/{id}/complete
  │
  ├─ Step 1: Rides.API  →  CompleteRide          ✅
  │
  ├─ Step 2: Payments.API  →  ChargeRider         ✅  (returns PaymentId)
  │
  ├─ Step 3: Payouts.API  →  PayDriver            ❌  (simulated failure)
  │
  └─ Compensate: Payments.API  →  RefundRider     ↩️
                                                   SAGA compensated ↩️
```


### SAGA flow — ChargeRider fails (no compensation needed)
```
POST /rides/{id}/complete
  │
  ├─ Step 1: Rides.API  →  CompleteRide          ✅
  │
  └─ Step 2: Payments.API  →  ChargeRider         ❌
                               Ride is done, payment failed — SAGA ends.
                               No prior steps to compensate.
```

### SAGA state record
```csharp
// MyRide.API — internal to the orchestrator
internal record RideCompletionSagaState(
    Guid RideId,
    Guid RiderId,
    Guid DriverId,
    decimal FareAmount,
    string FareCurrency,
    string TenantId,
    bool SimulatePaymentFailure,
    bool SimulatePayoutFailure
);
```

### Where it lives
`MyRide.API/Sagas/RideCompletionSaga.cs` — a plain class, no framework needed.

```csharp
public class RideCompletionSaga
{
    private readonly IRidesApi ridesApi;
    private readonly IPaymentsApi paymentsApi;
    private readonly IPayoutsApi payoutsApi;

    public async Task<SagaResult> Execute(RideCompletionSagaState state) { ... }
}
```

### SagaResult
```csharp
public record SagaResult(bool Succeeded, string? FailureReason, bool Compensated);
```

### BFF controller change
`CompleteRide` endpoint calls `RideCompletionSaga.Execute(...)` instead of directly calling `ridesApi.CompleteRide(...)`.

### Failure simulation
The `SimulateFailure` flag already exists in the UI per ride. The BFF receives it and passes it through to the SAGA state. The downstream services check this flag and throw if set — this already exists in the Payments and Payouts domain.

---

## 5. Driver Stream — Cross-Aggregate Invariance

> **See:** `docs/cross-aggregate-invariants.md` for full design rationale.

### Purpose
Prevent race condition where two riders are simultaneously assigned the same driver.

### New stream
`{tenantId}-driver-{driverId}` in EventStoreDB

### New events
- `DriverAssigned { DriverId, RideId, AssignedAt }` — appended on `StartRide`
- `DriverFreed { DriverId, RideId, FreedAt }` — appended on `CompleteRide` or `CancelRide`

### Enforcement in StartRideHandler
Before appending the ride event, append `DriverAssigned` to the driver stream at the expected revision. If two concurrent requests target the same driver, EventStoreDB's optimistic concurrency ensures only one succeeds.

### Risk note
Adds meaningful complexity. If time is tight before the demo, describe this pattern verbally using the `docs/cross-aggregate-invariants.md` notes rather than implementing it live.

---

## Deferred Items

### Request/Response → model folder
Pure folder reorganisation. No logic impact. Low CTO visibility. Do after the demo.

### Service Bus choreography
Replace BFF HTTP orchestration with event-driven choreography via Azure Service Bus. The infrastructure (publishers, topics) is already there. When Service Bus emulator is properly set up, this is a clean swap.

---

## Architecture at demo completion

```
UI (Angular)
  └── MyRide.API (BFF)
        ├── RideCompletionSaga (orchestrator)
        ├── Refit clients → Rides.API / Payments.API / Payouts.API / Drivers.API
        │
        ├── Rides.API
        │     ├── EventStoreDB (write — ride streams)
        │     ├── RideProjectionService (BackgroundService — reads EventStoreDB, writes SQL)
        │     └── SQL Server (read — RideReadModels)
        │
        ├── Payments.API
        │     └── EventStoreDB (write — payment streams)
        │
        ├── Payouts.API
        │     └── EventStoreDB (write — payout streams)
        │
        └── Drivers.API
              └── SQL Server (read — Drivers table)
```

---

## Key patterns to call out in the demo

| Pattern | Where |
|---|---|
| Domain-Driven Design | Aggregates, value objects, domain events in each bounded context |
| CQRS | EventStoreDB (write) separated from SQL Server (read) |
| Event Sourcing | Full event history in EventStoreDB, aggregates rebuilt by replaying events |
| Eventual Consistency | Projection service updates read model asynchronously from EventStoreDB |
| SAGA (Orchestration) | `RideCompletionSaga` in BFF — with compensation on payout failure |
| Cross-aggregate invariance | Driver stream + optimistic concurrency (or verbal walkthrough) |
| Clean Architecture | Domain → Application → Infrastructure → API, dependency rule enforced by project references |
| Multi-tenancy | TenantId scoped streams and tables throughout |
