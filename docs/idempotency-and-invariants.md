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

## Fixing the Rider Race Condition

### The practical solution: database unique constraint

Rather than implementing a distributed lock or a complex saga-level guard,
a unique index on the read model enforces the invariant at the storage level.
If two concurrent requests both pass the application check, the second insert
hits the constraint and fails. The database becomes the single arbiter.

```sql
CREATE UNIQUE INDEX UX_ActiveRidePerRider
ON rides.RideReadModels (RiderId, TenantId)
WHERE Status IN ('Requested', 'InProgress')
```

**What happens on conflict:**
- The second `StartRide` request reaches `SqlRideReadStore.Upsert`
- SQL Server rejects the insert with a unique constraint violation
- The exception propagates up through `StartRideHandler`
- The SAGA catches it, marks itself as compensating, and frees the driver
- The second request returns a clean error to the caller

**Why this is the right approach:**
- No distributed locking infrastructure needed
- Enforced at the database level regardless of how many API instances are running
- Naturally consistent with the read model the application already owns

---

## Where Idempotency Is Needed

### 1. Payments — Critical

**The risk:** The SAGA recovery job retries `ChargeRider` when a previous attempt
failed or timed out. If the original request actually succeeded but the response
was lost, the retry will charge the rider a second time.

**The scenario:**
```
SAGA Step 3:  POST /payments/charge  →  Payments.API processes payment ✓
                                     ←  Response lost (timeout)
SAGA marks PaymentFailed, saves to DB

Recovery job retries:
              POST /payments/charge  →  Payments.API processes payment AGAIN ✗
                                        Rider charged twice
```

**The fix:** Payments.API checks whether a payment already exists for the given
`rideId` before processing. If it does, it returns the existing result.

```
Idempotency key: rideId
Rule:           One payment record per rideId per tenant
On duplicate:   Return existing PaymentId with 200 OK — do not charge again
Storage:        Unique index on payments table (RideId, TenantId)
```

**Implementation in Payments.API:**
```csharp
// Before charging, check if payment already exists
var existing = await repository.GetByRideId(command.RideId, command.TenantId);
if (existing is not null)
{
    return existing.PaymentId; // already charged — return existing result
}

// Proceed with charge
```

---

### 2. Payouts — Critical (same reason as payments)

**The risk:** The recovery job retries `PayDriver` if a previous attempt failed
or timed out. Without idempotency, the driver receives a second payout.

**The fix:** Payouts.API checks whether a payout already exists for the given
`rideId` before processing.

```
Idempotency key: rideId
Rule:           One payout record per rideId per tenant
On duplicate:   Return existing PayoutId with 200 OK — do not pay again
Storage:        Unique index on payouts table (RideId, TenantId)
```

---

### 3. Driver Assign / Free — Partially Covered by EventStoreDB

**The situation:** EventStoreDB's optimistic concurrency control (OCC) already
provides some protection. When `AppendToStreamAsync` is called with an expected
revision, a concurrent write from another process will fail with
`WrongExpectedVersionException`.

**The gap:** If `AssignDriver` succeeds but the response is lost, the SAGA retries.
The second call loads the driver stream, tries to assign again, and the domain
logic needs to handle the case where the driver is already assigned to this
specific ride.

**The fix:** Make the domain method idempotent.

```csharp
// DriverAggregate.Assign
public void Assign(Guid rideId)
{
    if (CurrentRideId == rideId)
    {
        return; // already assigned to this ride — idempotent no-op
    }

    if (Status == DriverStatus.InProgress)
    {
        throw new InvalidOperationException("Driver is already on a ride.");
    }

    Raise(new DriverAssigned(Id, TenantId, rideId));
}
```

Same pattern applies to `Free` — if the driver is already free, treat as success.

```
Idempotency key: (driverId, rideId)
Rule:           Assigning the same driver to the same ride twice is a no-op
On duplicate:   Return success without emitting a new event
```

---

### 4. Ride Creation in EventStoreDB — Already Idempotent

The SAGA generates `rideId` upfront and stores it in `StartRideSagaState`
before any downstream calls. This means retries always use the same `rideId`.

`AppendToStreamAsync` with `StreamState.NoStream` will reject a second write
to a stream that already exists. The SAGA should treat `WrongExpectedVersionException`
on a `NoStream` append as "ride already created — success" and continue.

```
Idempotency key: rideId (pre-generated by SAGA)
Already handled: StreamState.NoStream rejects duplicates
Gap to close:   SAGA should not treat this as a fatal error — check if the
                existing stream matches the expected rideId and proceed
```

---

### 5. SAGA Creation — Prevent Duplicate SAGAs for the Same Rider

**The risk:** A user clicks "Start Ride" twice quickly. Two SAGA instances are
created concurrently. Both pass the `HasActiveRideForRider` check. The DB
unique constraint on the ride read model will ultimately catch one, but both
SAGAs will have assigned a driver before the conflict is discovered.

**The fix:** Introduce a SAGA-level idempotency key tied to the rider's intent.

```
Idempotency key: (riderId, tenantId) + short time window (e.g. 30 seconds)
Rule:           Only one pending StartRideSaga per rider at a time
On duplicate:   Return the existing saga's result
Storage:        Unique index on orchestrator.StartRideSagas (RiderId, TenantId)
                WHERE Status IN ('Pending', 'DriverAssigned')
```

---

## Summary Table

| Location | Problem | Idempotency Key | Mechanism |
|---|---|---|---|
| StartRideSaga creation | Two sagas for same rider | `(RiderId, TenantId)` | DB unique index on active sagas |
| `rides.RideReadModels` | Two active rides for same rider | `(RiderId, TenantId)` | DB unique constraint filtered on active status |
| Ride creation in EventStoreDB | Duplicate ride stream | `rideId` | `StreamState.NoStream` — treat duplicate stream as success |
| Driver assign / free | Retry assigns driver twice | `(driverId, rideId)` | Domain method checks existing state before raising event |
| `payments.Payments` | Rider charged twice on retry | `rideId` | Check existing payment before processing — return existing |
| `payouts.Payouts` | Driver paid twice on retry | `rideId` | Check existing payout before processing — return existing |

---

## Build Order Recommendation

| Priority | What | Why |
|---|---|---|
| 1 | DB unique constraint on `rides.RideReadModels` | Simplest, highest-impact, prevents data corruption |
| 2 | Idempotency in `Payments.API` | Real money — most critical |
| 3 | Idempotency in `Payouts.API` | Real money — same reason |
| 4 | `DriverAggregate.Assign / Free` idempotent | Closes the EventStoreDB retry gap |
| 5 | SAGA creation guard on `(RiderId, TenantId)` | Nice to have — DB constraint on ride covers most of this |

---

## Key Principle

> Idempotency makes retries safe.
> Invariant enforcement makes concurrent requests safe.
> You need both.
