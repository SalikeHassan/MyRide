# MyRide — SAGA Flows

## What is a SAGA?

A SAGA is a pattern for managing a multi-step business process that spans multiple services. Because each service has its own database, there is no shared transaction. A SAGA coordinates the steps and defines what to do when something goes wrong.

There are two recovery strategies:

- **Backward recovery** — undo completed steps using compensating actions (roll back to a clean state).
- **Forward recovery** — accept that some steps cannot be undone; instead retry or flag failures and keep moving.

---

## Flow 1: Start Ride — Backward Recovery SAGA

### Why backward?

When starting a ride, the driver is assigned before the ride is created. If the ride fails to be created, the driver must be freed. Nothing irreversible has happened yet, so a full rollback is possible.

### Steps and compensations

| Step | Service | Action | Compensating Action |
|------|---------|--------|-------------------|
| 1 | Drivers.API | Assign driver → `Driver.Status = InProgress` | Free driver → `Driver.Status = Available` |
| 2 | Rides.API | Create ride → written to EventStoreDB + SQL | None — it did not succeed |

### Happy path

```
MyRide.API
    │
    ├─► [1] POST /api/v1/drivers/{driverId}/assign       ✓
    │           Driver.Status = InProgress
    │
    ├─► [2] POST /api/v1/rides/start                     ✓
    │           Ride written to EventStoreDB
    │           RideReadModel written to SQL
    │
    └─► RideId returned to client ✓
```

### Failure at Step 2

```
MyRide.API
    │
    ├─► [1] POST /api/v1/drivers/{driverId}/assign       ✓
    │           Driver.Status = InProgress
    │
    ├─► [2] POST /api/v1/rides/start                     ✗ (500 / timeout)
    │
    └─► [Compensate 1] POST /api/v1/drivers/{driverId}/free
                Driver.Status = Available                ✓
                SAGA marked as Failed
```

### Failure during compensation

Compensation itself can fail (Drivers.API is down while we try to free the driver). This is handled by:

1. Persisting the SAGA state before each step.
2. A background **recovery job** that finds SAGAs stuck in `Compensating` state and retries the compensation.

### SAGA state transitions

```
Pending
  │
  ├─ Step 1 success ──► DriverAssigned
  │     │
  │     ├─ Step 2 success ──► Completed
  │     │
  │     └─ Step 2 failure ──► Compensating
  │               │
  │               ├─ Compensation success ──► Failed (clean)
  │               └─ Compensation failure ──► CompensationFailed (needs manual review)
  │
  └─ Step 1 failure ──► Failed (clean — nothing to compensate)
```

---

## Flow 2: Complete Ride — Forward Recovery SAGA

### Why forward?

Once a ride is marked complete, it cannot be "un-completed." The service was delivered. Steps downstream (freeing the driver, charging the rider, paying the driver) cannot reverse that fact. Instead of rolling back, we retry failures and flag anything that cannot be resolved automatically.

### Steps and recovery strategy

| Step | Service | Action | On Failure |
|------|---------|--------|-----------|
| 1 | Rides.API | Complete ride → `Ride.Status = Completed` | Retry (transient) / Abort (non-transient) |
| 2 | Drivers.API | Free driver → `Driver.Status = Available` | Retry — idempotent, must eventually succeed |
| 3 | Payments.API | Charge rider | Retry up to N times, then mark `PaymentFailed` |
| 4 | Payouts.API | Pay driver | Retry up to N times, then mark `PayoutFailed` |

### Happy path

```
MyRide.API
    │
    ├─► [1] POST /api/v1/rides/{rideId}/complete         ✓
    │           Ride.Status = Completed
    │
    ├─► [2] POST /api/v1/drivers/{driverId}/free         ✓
    │           Driver.Status = Available
    │
    ├─► [3] POST /api/v1/payments/charge                 ✓
    │           Payment record created, rider charged
    │
    ├─► [4] POST /api/v1/payouts/pay                     ✓
    │           Payout record created, driver paid
    │
    └─► Success ✓
```

### Failure at Step 2 (free driver)

```
    ├─► [1] Rides.API: complete ride                     ✓
    ├─► [2] Drivers.API: free driver                     ✗
    │
    └─► Retry Step 2 (up to 3 times with backoff)
              │
              ├─ Retry succeeds ──► continue to Step 3
              └─ All retries fail ──► SAGA status = FreeDriverFailed
                                       Recovery job retries later
```

Driver freeing is safe to retry because it is idempotent — freeing an already-free driver is a no-op.

### Failure at Step 3 (payment)

```
    ├─► [1] Rides completed                              ✓
    ├─► [2] Driver freed                                 ✓
    ├─► [3] Payments.API: charge rider                   ✗
    │
    └─► Retry Step 3 (up to 3 times)
              │
              ├─ Retry succeeds ──► continue to Step 4
              └─ All retries fail ──► SAGA status = PaymentFailed
                                       Flagged for manual review
                                       Do NOT refund or revert ride
```

The ride is complete — the service was delivered. Payment failure is a billing concern, not a reason to undo the ride.

### Failure at Step 4 (payout)

```
    ├─► [1–3] All succeeded                              ✓
    ├─► [4] Payouts.API: pay driver                      ✗
    │
    └─► Retry Step 4 (up to 3 times)
              │
              ├─ Retry succeeds ──► Completed
              └─ All retries fail ──► SAGA status = PayoutFailed
                                       Flagged for manual review
                                       Payment is NOT reversed
```

The rider was charged for a service they received. Payout failure is an internal finance concern.

### SAGA state transitions

```
Pending
  │
  ├─ Step 1 success ──► RideCompleted
  │     │
  │     ├─ Step 2 success ──► DriverFreed
  │     │     │
  │     │     ├─ Step 3 success ──► PaymentCharged
  │     │     │     │
  │     │     │     ├─ Step 4 success ──► Completed ✓
  │     │     │     └─ Step 4 fails all retries ──► PayoutFailed ⚠
  │     │     │
  │     │     └─ Step 3 fails all retries ──► PaymentFailed ⚠
  │     │
  │     └─ Step 2 fails all retries ──► FreeDriverFailed ⚠
  │
  └─ Step 1 failure ──► Failed (ride was not completed)
```

States marked ⚠ are persisted and picked up by the recovery job for retry or escalation.

---

## Persistence

SAGA state is stored in SQL Server inside MyRide.API's own schema (`orchestrator`). Each SAGA instance is a row.

### StartRideSaga table

| Column | Type | Description |
|--------|------|-------------|
| SagaId | uniqueidentifier | Primary key |
| TenantId | nvarchar(100) | |
| RideId | uniqueidentifier | Target ride |
| DriverId | uniqueidentifier | Target driver |
| RiderId | uniqueidentifier | |
| Status | nvarchar(50) | Current state |
| CreatedAt | datetime2 | |
| UpdatedAt | datetime2 | |
| FailureReason | nvarchar(500) | Set on failure |

### CompleteRideSaga table

| Column | Type | Description |
|--------|------|-------------|
| SagaId | uniqueidentifier | Primary key |
| TenantId | nvarchar(100) | |
| RideId | uniqueidentifier | |
| DriverId | uniqueidentifier | |
| RiderId | uniqueidentifier | |
| FareAmount | decimal(18,2) | |
| FareCurrency | nvarchar(10) | |
| Status | nvarchar(50) | Current state |
| RetryCount | int | Steps 2–4 retry attempts |
| CreatedAt | datetime2 | |
| UpdatedAt | datetime2 | |
| FailureReason | nvarchar(500) | |

---

## Resilience: The Recovery Job

A `BackgroundService` runs in MyRide.API on a timer (e.g., every 30 seconds). It finds SAGA instances that are stuck and retries them.

```
Recovery job finds:
  - StartRideSaga where Status = Compensating  → retry FreeDriver compensation
  - CompleteRideSaga where Status = FreeDriverFailed → retry FreeDriver
  - CompleteRideSaga where Status = PaymentFailed    → retry payment
  - CompleteRideSaga where Status = PayoutFailed     → retry payout
  - Any SAGA where UpdatedAt < (now - 10 min)        → assume stuck, retry current step
```

---

## Summary

| | Start Ride | Complete Ride |
|---|---|---|
| Recovery type | Backward (compensate) | Forward (retry + flag) |
| Can fully roll back? | Yes | No |
| Compensating actions | Free driver | None — retries only |
| Stuck SAGA resolution | Retry compensation | Retry failed step |
| Manual intervention needed | Only if compensation fails repeatedly | If payment/payout exhausts retries |
