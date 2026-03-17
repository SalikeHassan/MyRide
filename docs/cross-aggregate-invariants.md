# Cross-Aggregate Invariants in Event Sourcing

## The Problem

In an event-sourced system, **optimistic concurrency is enforced per stream**. This works perfectly for invariants that live within a single aggregate (e.g., "a ride cannot be cancelled if it is already completed"). However, some business rules span across multiple aggregates — these are called **cross-aggregate invariants**.

### Real example from MyRide

> "A driver can only be assigned to one active ride at a time."

This invariant touches two aggregates: the **Driver** and the **Ride**. 
When two riders simultaneously request a ride, both go through the same flow:

1. `GET /drivers/available` — both get the same driver (SQL read model hasn't updated yet)
2. Both call `StartRide` with the same driver
3. Both create brand new ride streams — both succeed with `StreamState.NoStream`

```
Stream: tenant1-ride-abc  →  RideStarted (DriverA)  ✅ succeeds
Stream: tenant1-ride-xyz  →  RideStarted (DriverA)  ✅ succeeds
```

**Driver A is now double-booked.** EventStoreDB had no way to know — both streams were independent.

---

## Why the Ride Stream Cannot Solve It

Adding a `DriverAssigned` event to the ride stream does not help. At the moment ride2's stream is being written, EventStoreDB has zero awareness of ride1's stream. There is no native "check stream X before writing to stream Y" mechanism.

> **Core principle:** To enforce an invariant with optimistic concurrency, all competing writers must contend on the **same stream**.

This is why within-aggregate invariants are easy — every command on the same aggregate targets the same stream. Cross-aggregate invariants are harder because they require a shared serialisation point.

---

## The Driver Stream Solution

Create a dedicated stream per driver:

```
{tenantId}-driver-{driverId}
```

When `StartRide` runs, **before** appending the ride event, append a `DriverAssigned` event to the driver's stream at a known expected revision:

```
Req 1:  tenant1-driver-driverA  @  StreamState.NoStream  →  ✅ DriverAssigned (revision 0)
Req 2:  tenant1-driver-driverA  @  StreamState.NoStream  →  ❌ WrongExpectedVersionException
```

EventStoreDB's optimistic concurrency guarantees only one writer wins. The second request catches the exception and retries with a different driver.

When the ride completes or is cancelled, append `DriverFreed` to the driver stream. The next assignment then expects revision 0 again (or the current revision of the driver stream).

### What this stream holds

The driver stream does not need to replicate all driver data. It only needs enough events to establish the "is this driver currently assigned?" invariant:

- `DriverAssigned { RideId, AssignedAt }`
- `DriverFreed { RideId, FreedAt }`

---

## All Options for Cross-Aggregate Invariants

| Approach | Mechanism | Trade-off |
|---|---|---|
| **Driver stream + OCC** | Dedicated stream per driver; EventStoreDB enforces at write time | Extra streams, but guarantee is at the DB level — no race window |
| **Saga / Process Manager** | Allow double-booking; detect conflict in projection; emit compensating `RideCancelled` | Fully eventual, more code, visible to the user |
| **Distributed lock (Redis)** | Acquire lock on `driverId` before assignment, release after append | External dependency; not event-sourced; still works |
| **Single assignment stream** | One `{tenantId}-driver-assignments` stream; all assignments serialised through it | Simple OCC but a write bottleneck under high load |
| **Read model guard (current)** | Check SQL read model before writing; reject if driver already active | Best-effort only — race window exists between check and write |

---

## Eventual Consistency and the Projection Trade-off

Moving to a projection service (EventStoreDB subscription → SQL update) makes this problem more visible. In the dual-write model, the guard check and the write happen in the same handler — the race window is tiny. With a projection, the read model update is delayed, making the guard check on SQL less reliable.

This is why the **guard must move to the write side** when adopting projections:

```
Before (dual write):   handler → [append event] + [update SQL]   ← guard on SQL, small window
After (projection):    handler → [append event only]             ← guard on SQL, larger window
Correct approach:      handler → [append to driver stream (OCC)] + [append ride event]
```

The projection then handles read model updates asynchronously without being involved in enforcing the invariant at all.

---

## Summary

| Scenario | EventStoreDB can protect you? |
|---|---|
| Invariant within one aggregate | ✅ Yes — OCC on the same stream |
| Invariant across two aggregates | ❌ No — streams are independent |
| Cross-aggregate invariant (with driver stream) | ✅ Yes — both writers contend on the driver stream |

The key insight: **the aggregate boundary defines the consistency boundary**. If your invariant spans two aggregates, you either need to rethink the boundary (merge them), introduce a coordination stream, or accept eventual consistency with compensation.

---

*Context: MyRide — multi-tenant ride payment platform. Discussed in the context of migrating from dual-write to EventStoreDB projection service.*
