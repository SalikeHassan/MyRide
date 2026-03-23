# Domain Quick Reference

> Interview reference — events, SAGA states, idempotency. Created 2026-03-23.

---

## 1. Events per Aggregate

### RideAggregate
Stream name: `{tenantId}-ride-{rideId}`

| Event | Raised by | Key payload |
|---|---|---|
| `RideRequested` | `RideAggregate.Create(...)` | `RideId`, `RiderId`, `DriverId`, `DriverName`, `FareAmount`, `FareCurrency`, `PickupLat/Lng`, `DropoffLat/Lng` |
| `RideAccepted` | `RideAggregate.Accept()` | `RideId`, `DriverId` |
| `RideCompleted` | `RideAggregate.Complete()` | `RideId` |
| `RideCancelled` | `RideAggregate.Cancel(reason)` | `RideId`, `Reason` |

### DriverAggregate
Stream name: `{tenantId}-driver-{driverId}`

| Event | Raised by | Key payload |
|---|---|---|
| `DriverAssigned` | `DriverAggregate.Assign(rideId)` | `DriverId`, `RideId` |
| `DriverFreed` | `DriverAggregate.Free()` | `DriverId` |

### PaymentAggregate
Stream name: `{tenantId}-payment-{rideId}` *(keyed by RideId, not PaymentId)*

| Event | Raised by | Key payload |
|---|---|---|
| `RiderCharged` | `PaymentAggregate.Charge(command)` | `PaymentId`, `RideId`, `PayerId`, `PayeeId`, `Amount`, `Currency` |
| `RiderRefunded` | `PaymentAggregate.Refund()` | `PaymentId`, `RideId` |

### PayoutAggregate
Stream name: `{tenantId}-payout-{rideId}` *(keyed by RideId, not PayoutId)*

| Event | Raised by | Key payload |
|---|---|---|
| `DriverPaid` | `PayoutAggregate.Pay(command)` | `PayoutId`, `RideId`, `RecipientId`, `Amount`, `Currency` |

---

## 2. SAGA Status State Machines

**Status enum values:**
`Pending` → `DriverAssigned` → `RideCreated` / `Completed`
`AssignDriverFailed` / `RideCreationFailed` → `Compensating` → `Compensated`

### CompleteRideSaga (Forward Recovery — retry until success)

**Status enum values:**
`Pending` → `RideCompleted` → `DriverFreed` → `PaymentCharged` → `Completed`
Failure statuses (all retried): `FreeDriverFailed` | `PaymentFailed` | `PayoutFailed`

**Recovery job logic (Resume):**
```
if status is RideCompleted  OR FreeDriverFailed  → retry FreeDriver
if status is DriverFreed    OR PaymentFailed     → retry ChargeRider
if status is PaymentCharged OR PayoutFailed      → retry PayDriver
```

---

## 3. Idempotency Implementations

### 3a. Driver Assignment — `DriverAggregate.Assign(rideId)`
**File:** `Drivers.Domain/Aggregates/DriverAggregate.cs`

```csharp
public void Assign(Guid rideId)
{
    if (IsAssigned && CurrentRideId == rideId) { return; }   // ← idempotent
    if (IsAssigned) { throw new InvalidOperationException(...); }
    RaiseEvent(new DriverAssigned(Id, rideId, TenantId, DateTime.UtcNow));
}
```
Guard: if already assigned to **same** ride → no-op. If assigned to a **different** ride → throw.

---

### 3b. Ride Creation — `StartRideHandler.Handle`
**File:** `Rides.Application/Handlers/StartRideHandler.cs`

```csharp
if (await eventStore.Exists(command.RideId, command.TenantId))
{
    return;   // ← idempotent
}
```
Guard: checks `IRideEventStore.Exists(rideId, tenantId)` — stream presence in EventStoreDB.

---

### 3c. Payment Charge — `ChargeRiderHandler.Handle`
**File:** `Payments.Application/Handlers/ChargeRiderHandler.cs`

```csharp
if (await eventStore.ExistsByRideId(command.RideId, command.TenantId))
{
    var existing = await eventStore.LoadByRideId(command.RideId, command.TenantId);
    return existing.Id;   // ← idempotent — return existing PaymentId
}
```
Guard: checks `IPaymentEventStore.ExistsByRideId` — payment stream keyed by `rideId`.

---

### 3d. Driver Payout — `PayDriverHandler.Handle`
**File:** `Payouts.Application/Handlers/PayDriverHandler.cs`

```csharp
if (await eventStore.ExistsByRideId(command.RideId, command.TenantId))
{
    var existing = await eventStore.LoadByRideId(command.RideId, command.TenantId);
    return existing.Id;   // ← idempotent — return existing PayoutId
}
```
Guard: checks `IPayoutEventStore.ExistsByRideId` — payout stream keyed by `rideId`.

---

### 3e. Active Ride Uniqueness — SQL filtered unique index (read side)
**File:** `Rides.Infrastructure/Persistence/RidesReadDbContext.cs`

```sql
CREATE UNIQUE INDEX UX_ActiveRidePerRider
  ON RideReadModels (RiderId, TenantId)
  WHERE Status IN ('Requested', 'InProgress')
```
Invariant: a rider cannot have two active rides simultaneously. Enforced at the DB level — no code path can create a second row for the same rider in an active status.

---

### 3f. StartRideSaga Creation Guard — SQL filtered unique index
**File:** `MyRide.Infrastructure/Persistence/OrchestratorDbContext.cs`

```sql
CREATE UNIQUE INDEX UX_ActiveStartRideSagaPerRider
  ON StartRideSagas (RiderId, TenantId)
  WHERE Status IN ('Pending', 'DriverAssigned')
```
Invariant: only one in-flight `StartRideSaga` per rider at a time. Prevents concurrent ride requests from creating duplicate SAGAs before the first one completes.

---
