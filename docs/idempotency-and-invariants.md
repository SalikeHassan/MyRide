# MyRide — Idempotency & Cross-Aggregate Invariants

---

## The Core Distinction

These two concepts are often confused but solve fundamentally different problems.

**Cross-Aggregate Invariant**
A business rule that must hold true across multiple aggregates simultaneously.
> "A rider can only have one active ride at a time"

**Idempotency**
An operation that produces the same result whether it is executed once or many times.
> "Retrying a payment request does not charge the rider twice"

Idempotency **cannot** replace cross-aggregate invariance. They must coexist.

---

## The Race Condition Problem

### Scenario: Two concurrent StartRide requests for the same rider

```
Time ──────────────────────────────────────────────────────────►

Request A:  [check HasActiveRide → false]  [create ride A]  ✓
Request B:        [check HasActiveRide → false]  [create ride B]  ✓

Result: Two active rides for the same rider — invariant violated
```

Request B passes the application-level check because Request A has not yet
committed to the database. This is a classic read-then-write race condition.

### Why idempotency does not fix this

Idempotency protects against the **same request** arriving more than once
(e.g., a network retry). It does not protect against **two different requests**
arriving concurrently for the same rider. Those are genuinely distinct
operations that both appear valid at the time of the check.

---

## What Was Built

### 1. `rides.RideReadModels` — Filtered Unique Index

**Problem:** Two concurrent `StartRide` requests both pass the application-level
`HasActiveRideForRider` check before either commits.

**Solution:** A unique SQL index enforces the invariant at the database level.
The second insert hits the constraint and fails — no distributed locking needed.

```sql
-- Applied via migration: AddActiveRideUniqueConstraint
CREATE UNIQUE INDEX [UX_ActiveRidePerRider]
ON [rides].[RideReadModels] ([RiderId], [TenantId])
WHERE [Status] IN ('Requested', 'InProgress')
```

**Files changed:**
- `Rides.Infrastructure/Persistence/RidesReadDbContext.cs` — declared the index in `OnModelCreating`
- `Rides.Infrastructure/Migrations/20260318…_AddActiveRideUniqueConstraint.cs` — migration

**What happens on conflict:**
- The second `StartRide` request reaches `SqlRideReadStore.Upsert`
- SQL Server rejects the insert with a unique constraint violation
- The exception propagates to the SAGA, which marks itself `Compensating` and frees the driver
- The second request returns a clean error to the caller

---

### 2. `Payments.API` — Payment Idempotency via `rideId`-keyed Stream

**Problem:** The SAGA recovery job retries `ChargeRider` when a previous attempt
timed out. If the original charge succeeded but the response was lost, the retry
charges the rider a second time.

**Solution:** The EventStoreDB stream for a payment is keyed by `rideId` instead of
`paymentId`. Before charging, the handler checks whether that stream already exists.
If it does, it returns the existing `PaymentId` without charging again.

**Key:** `rideId` (one payment stream per ride per tenant)
**On duplicate:** returns existing `PaymentId` with the same 200 OK response

```csharp
// Payments.Application/Handlers/ChargeRiderHandler.cs
public async Task<Guid> Handle(ChargeRiderCommand command)
{
    if (await eventStore.ExistsByRideId(command.RideId, command.TenantId))
    {
        var existing = await eventStore.LoadByRideId(command.RideId, command.TenantId);
        return existing.Id;  // already charged — return existing PaymentId
    }

    var payment = PaymentAggregate.Charge(command);
    await eventStore.AppendWithRideId(payment, command.RideId);
    // ...
    return payment.Id;
}
```

**EventStoreDB stream name:** `{tenantId}-payment-{rideId}`

**Files changed:**
- `Payments.Domain/Commands/ChargeRiderCommand.cs` — added `RideId`; `PaymentId` now generated inside the command
- `Payments.Application/Ports/IPaymentEventStore.cs` — added `ExistsByRideId`, `LoadByRideId`, `AppendWithRideId`
- `Payments.Application/Handlers/ChargeRiderHandler.cs` — idempotency check; returns `Task<Guid>`
- `Payments.Infrastructure/Persistence/EventStoreDbPaymentEventStore.cs` — implemented new port methods
- `Payments.API/Models/Requests/ChargeRiderRequest.cs` — added `RideId`
- `Payments.API/Controllers/PaymentsController.cs` — passes `RideId`, uses returned `paymentId`
- `Common.Infrastructure/EventStoreDbEventStore.cs` — added `AppendEvents(aggregate, Guid streamId)` overload
- `MyRide.Infrastructure/Models/ChargeRiderRequest.cs` — added `RideId`
- `MyRide.Application/Ports/IDownstreamPaymentsClient.cs` — added `rideId` parameter
- `MyRide.Infrastructure/Clients/Adapters/PaymentsApiClient.cs` — passes `rideId`
- `MyRide.Application/Sagas/CompleteRideSaga.cs` — passes `saga.RideId` to `ChargeRider`

---

### 3. `Payouts.API` — Payout Idempotency via `rideId`-keyed Stream

**Problem:** Same as payments — SAGA retry of `PayDriver` pays the driver twice
if the original payout succeeded but the response was lost.

**Solution:** Identical pattern to payments. EventStoreDB stream keyed by `rideId`.
Handler checks existence before processing.

**Key:** `rideId` (one payout stream per ride per tenant)
**On duplicate:** returns existing `PayoutId` with the same 200 OK response

```csharp
// Payouts.Application/Handlers/PayDriverHandler.cs
public async Task<Guid> Handle(PayDriverCommand command)
{
    if (await eventStore.ExistsByRideId(command.RideId, command.TenantId))
    {
        var existing = await eventStore.LoadByRideId(command.RideId, command.TenantId);
        return existing.Id;  // already paid — return existing PayoutId
    }

    var payout = PayoutAggregate.Pay(command);
    await eventStore.AppendWithRideId(payout, command.RideId);
    // ...
    return payout.Id;
}
```

**EventStoreDB stream name:** `{tenantId}-payout-{rideId}`

**Files changed:**
- `Payouts.Domain/Commands/PayDriverCommand.cs` — added `RideId`; `PayoutId` generated inside the command
- `Payouts.Application/Ports/IPayoutEventStore.cs` — added `ExistsByRideId`, `LoadByRideId`, `AppendWithRideId`
- `Payouts.Application/Handlers/PayDriverHandler.cs` — idempotency check; returns `Task<Guid>`
- `Payouts.Infrastructure/Persistence/EventStoreDbPayoutEventStore.cs` — implemented new port methods
- `Payouts.API/Models/Requests/PayDriverRequest.cs` — added `RideId`
- `Payouts.API/Controllers/PayoutsController.cs` — passes `RideId`, returns `PayoutId`
- `MyRide.Infrastructure/Models/PayDriverRequest.cs` — added `RideId`
- `MyRide.Infrastructure/Models/PayDriverResponse.cs` — new; carries `PayoutId` back to orchestrator
- `MyRide.Infrastructure/Clients/Refit/IPayoutsApi.cs` — return type changed to `Task<PayDriverResponse>`
- `MyRide.Application/Ports/IDownstreamPayoutsClient.cs` — added `rideId`; return type `Task<Guid>`
- `MyRide.Infrastructure/Clients/Adapters/PayoutsApiClient.cs` — passes `rideId`, returns `payoutId`
- `MyRide.Application/Sagas/CompleteRideSaga.cs` — passes `saga.RideId` to `PayDriver`

---

### 4. `DriverAggregate.Assign` — Domain-Level Idempotency

**Problem:** If `AssignDriver` succeeds in EventStoreDB but the response is lost,
the SAGA retry calls `AssignDriver` again. Without protection, the second call throws
`"Driver already has an active ride"` — which the SAGA treats as a failure and begins
compensation, incorrectly freeing a driver that is legitimately assigned.

**Solution:** The aggregate checks whether the driver is already assigned to the
**same** ride. If so, it returns silently — no event raised, no error thrown.

```csharp
// Drivers.Domain/Aggregates/DriverAggregate.cs
public void Assign(Guid rideId)
{
    if (IsAssigned && CurrentRideId == rideId)
    {
        return;  // already assigned to this ride — idempotent no-op
    }

    if (IsAssigned)
    {
        throw new InvalidOperationException($"Driver {Id} already has an active ride.");
    }

    RaiseEvent(new DriverAssigned(Id, rideId, TenantId, DateTime.UtcNow));
}
```

`CurrentRideId` is populated from the `DriverAssigned` event during rehydration
and cleared when `DriverFreed` is applied.

**Key:** `(driverId, rideId)` pair
**On duplicate:** no-op — returns success without emitting a new event

**Files changed:**
- `Drivers.Domain/Aggregates/DriverAggregate.cs` — added `CurrentRideId` property, updated `Assign`, updated `Apply(DriverAssigned)` and `Apply(DriverFreed)`

---

### 5. Ride Creation in EventStoreDB — Stream Existence Check

**Problem:** The SAGA pre-generates `rideId` before calling `Rides.API`. On retry,
it calls `StartRide` again with the same `rideId`. `StartRideHandler` calls
`AppendToStreamAsync` with `StreamState.NoStream`, which throws
`WrongExpectedVersionException` if the stream already exists. Without handling,
the SAGA sees this as a failure and begins compensation.

**Solution:** `StartRideHandler` checks whether the ride stream already exists
before doing any work. If it does, the ride was already created — return immediately.

```csharp
// Rides.Application/Handlers/StartRideHandler.cs
public async Task Handle(StartRideCommand command)
{
    if (await eventStore.Exists(command.RideId, command.TenantId))
    {
        return;  // ride already created — idempotent success
    }

    // ... proceed with normal creation
}
```

**Key:** `rideId` (pre-generated by SAGA and stored in `StartRideSagaState`)
**On duplicate:** early return — no event raised, no read model update attempted

**Files changed:**
- `Rides.Application/Ports/IRideEventStore.cs` — added `Task<bool> Exists(Guid rideId, string tenantId)`
- `Rides.Infrastructure/Persistence/EventStoreDbRideEventStore.cs` — implemented `Exists` via `StreamExists`
- `Rides.Application/Handlers/StartRideHandler.cs` — existence check at the top of `Handle`

---

### 6. `StartRideSaga` Creation Guard — Prevent Duplicate SAGAs

**Problem:** A user double-taps "Start Ride". Two SAGA instances are created
concurrently. Both may assign a driver before the ride unique constraint fires.
This wastes one driver assignment and requires unnecessary compensation.

**Solution:** A filtered unique index on the SAGA table ensures only one active
SAGA per rider at a time. The second insert fails fast at the database level,
before any downstream calls are made.

```sql
-- Applied via migration: AddStartRideSagaCreationGuard
CREATE UNIQUE INDEX [UX_ActiveStartRideSagaPerRider]
ON [orchestrator].[StartRideSagas] ([RiderId], [TenantId])
WHERE [Status] IN ('Pending', 'DriverAssigned')
```

**Key:** `(RiderId, TenantId)` while SAGA is in an active state
**On conflict:** second SAGA insert fails immediately — no driver is assigned

**Files changed:**
- `MyRide.Infrastructure/Persistence/OrchestratorDbContext.cs` — declared the index in `OnModelCreating`
- `MyRide.Infrastructure/Migrations/20260318…_AddStartRideSagaCreationGuard.cs` — migration

---

## Infrastructure Change: `AppendEvents` Overload

Payments and Payouts idempotency required writing aggregate events to a stream
keyed by `rideId` rather than the aggregate's own `Id`. A second overload was
added to the shared base class:

```csharp
// Common.Infrastructure/EventStoreDbEventStore.cs

// Original — stream keyed by aggregate.Id
protected Task AppendEvents(TAggregate aggregate)

// New — stream keyed by an explicit streamId (e.g. rideId)
protected Task AppendEvents(TAggregate aggregate, Guid streamId)
```

Both delegate to a private `AppendEventsToStream(aggregate, streamName)` method.
This keeps all stream-naming logic in one place.

---

## Summary Table

| # | Location | Problem | Key | Mechanism |
|---|---|---|---|---|
| 1 | `rides.RideReadModels` | Two active rides for same rider | `(RiderId, TenantId)` | SQL filtered unique index on active status |
| 2 | `Payments.API` | Rider charged twice on SAGA retry | `rideId` | EventStoreDB stream existence check before charge |
| 3 | `Payouts.API` | Driver paid twice on SAGA retry | `rideId` | EventStoreDB stream existence check before payout |
| 4 | `DriverAggregate.Assign` | SAGA retry assigns driver twice | `(driverId, rideId)` | Domain method checks `CurrentRideId` — no-op if same ride |
| 5 | Ride creation in EventStoreDB | SAGA retry creates duplicate ride stream | `rideId` | `IRideEventStore.Exists` check before appending |
| 6 | `orchestrator.StartRideSagas` | Double-tap creates two SAGAs | `(RiderId, TenantId)` | SQL filtered unique index on active SAGA status |

---

## Build Order (Implemented)

| Priority | What | Why |
|---|---|---|
| 1 | `rides.RideReadModels` unique index | Simplest, highest-impact, prevents data corruption |
| 2 | `Payments.API` idempotency | Real money — most critical |
| 3 | `Payouts.API` idempotency | Real money — same reason |
| 4 | `DriverAggregate.Assign / Free` idempotent | Closes EventStoreDB retry gap |
| 5 | `StartRideHandler` existence check | Prevents spurious compensation on ride-creation retry |
| 6 | `StartRideSaga` creation guard | Stops double-SAGA before any downstream calls |

---

## Key Principle

> Idempotency makes retries safe.
> Invariant enforcement makes concurrent requests safe.
> You need both.
