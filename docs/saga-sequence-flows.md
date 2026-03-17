# MyRide — SAGA Sequence Flows

---

## Flow 1: Start Ride SAGA

### Entry Point
```
Client  →  POST /api/v1/rides/start  (MyRide.API)
           Headers: X-Tenant-Id: tenant1
           Body:    { fareAmount, fareCurrency, pickupLat, pickupLng, dropoffLat, dropoffLng }
```

---

### Full Sequence

```
Client
  │
  ▼
MyRide.API  RidesController.StartRide
  │
  │  ── Get available driver ──────────────────────────────────────────────────────────────────
  │
  │  IDownstreamDriversClient.GetAvailableDriver(tenantId)
  │    └─► DriversApiClient.GetAvailableDriver
  │          └─► Refit: IDriversApi.GetAvailableDriver
  │                └─► GET http://localhost:5042/api/v1/drivers/available
  │                      Header: X-Tenant-Id: tenant1
  │                      │
  │                      ▼
  │                 Drivers.API  DriversController.GetAvailable
  │                      └─► GetAvailableDriverHandler
  │                            └─► SqlDriverRepository.GetAvailable
  │                                  └─► SELECT * FROM drivers.Drivers
  │                                        WHERE TenantId = 'tenant1'
  │                                        AND Status = 'Available'
  │                      │
  │                      ▼  Response: 200 OK
  │                 { id: "aaa...001", name: "James Carter", phone: "+44 7700 900001" }
  │          └─► maps to AvailableDriverResult { Id, Name, Phone }
  │
  │  if driver is null → return 409 Conflict "No drivers available"
  │
  │  riderId = Guid.NewGuid()
  │
  │  ── Execute StartRideSaga ──────────────────────────────────────────────────────────────────
  │
  ├─► StartRideSaga.Execute(driverId, riderId, driverName, tenantId, fare...)
  │
  │   rideId = Guid.NewGuid()
  │   saga   = StartRideSagaState.Create(...)   [Status: Pending]
  │   repository.Save(saga)
  │     └─► INSERT INTO orchestrator.StartRideSagas
  │
  │   ════════════════════════════════════════════════
  │   STEP 1 — Assign Driver
  │   ════════════════════════════════════════════════
  │
  │   IDownstreamDriversClient.AssignDriver(driverId, rideId, tenantId)
  │     └─► DriversApiClient.AssignDriver
  │           └─► Refit: IDriversApi.AssignDriver(driverId, body)
  │                 └─► POST http://localhost:5042/api/v1/drivers/{driverId}/assign
  │                       Body: { rideId: "...", tenantId: "tenant1" }
  │                       │
  │                       ▼
  │                  Drivers.API  DriversController.Assign
  │                       └─► AssignDriverHandler
  │                             ├─► EventStoreDbDriverEventStore.Load(driverId, tenantId)
  │                             │     └─► EventStoreDB: READ tenant1-driver-{driverId}
  │                             ├─► driver.Assign(rideId)
  │                             │     └─► emits DriverAssigned event
  │                             ├─► EventStoreDbDriverEventStore.Append(driver)
  │                             │     └─► EventStoreDB: APPEND tenant1-driver-{driverId}
  │                             └─► SqlDriverRepository.UpdateStatus(driverId, InProgress, tenantId)
  │                                   └─► UPDATE drivers.Drivers SET Status = 'InProgress'
  │                       │
  │                       ▼  Response: 200 OK
  │
  │   saga.MarkDriverAssigned()   [Status: DriverAssigned]
  │   repository.Save(saga)
  │     └─► UPDATE orchestrator.StartRideSagas SET Status = 'DriverAssigned'
  │
  │   ════════════════════════════════════════════════
  │   STEP 2 — Create Ride
  │   ════════════════════════════════════════════════
  │
  │   IDownstreamRidesClient.StartRide(StartRideData, tenantId)
  │     └─► RidesApiClient.StartRide
  │           └─► Refit: IRidesApi.StartRide(body, tenantId)
  │                 └─► POST http://localhost:5122/api/v1/rides/start
  │                       Header: X-Tenant-Id: tenant1
  │                       Body:   { rideId, riderId, driverId, driverName,
  │                                 fareAmount, fareCurrency,
  │                                 pickupLat, pickupLng, dropoffLat, dropoffLng }
  │                       │
  │                       ▼
  │                  Rides.API  RidesController.StartRide
  │                       └─► StartRideHandler
  │                             ├─► SqlRideReadStore.HasActiveRideForRider(riderId, tenantId)
  │                             │     └─► SELECT FROM rides.RideReadModels
  │                             ├─► RideAggregate.Start(command)
  │                             │     └─► emits RideStarted event
  │                             ├─► EventStoreDbRideEventStore.Append(ride)
  │                             │     └─► EventStoreDB: APPEND tenant1-ride-{rideId}
  │                             └─► SqlRideReadStore.Upsert(RideReadModel)
  │                                   └─► INSERT INTO rides.RideReadModels
  │                       │
  │                       ▼  Response: 200 OK
  │                 { rideId: "...", message: "Ride requested." }
  │
  │   saga.MarkCompleted()   [Status: Completed]
  │   repository.Save(saga)
  │     └─► UPDATE orchestrator.StartRideSagas SET Status = 'Completed'
  │
  ▼
MyRide.API → return 200 OK
  { rideId, riderId, driverId, driverName, message: "Ride started." }
```

---

### Compensation Path (Step 2 fails)

```
  │   STEP 2 fails (Rides.API returns 5xx / timeout)
  │
  │   saga.MarkCompensating("...")   [Status: Compensating]
  │   repository.Save(saga)
  │     └─► UPDATE orchestrator.StartRideSagas SET Status = 'Compensating'
  │
  │   ════════════════════════════════════════════════
  │   COMPENSATE STEP 1 — Free Driver
  │   ════════════════════════════════════════════════
  │
  │   IDownstreamDriversClient.FreeDriver(driverId, rideId, tenantId)
  │     └─► DriversApiClient.FreeDriver
  │           └─► Refit: IDriversApi.FreeDriver(driverId, body)
  │                 └─► POST http://localhost:5042/api/v1/drivers/{driverId}/free
  │                       Body: { rideId: "...", tenantId: "tenant1" }
  │                       │
  │                       ▼
  │                  Drivers.API  DriversController.Free
  │                       └─► FreeDriverHandler
  │                             ├─► EventStoreDbDriverEventStore.Load(driverId, tenantId)
  │                             ├─► driver.Free(rideId)
  │                             │     └─► emits DriverFreed event
  │                             ├─► EventStoreDbDriverEventStore.Append(driver)
  │                             └─► SqlDriverRepository.UpdateStatus(driverId, Available, tenantId)
  │                                   └─► UPDATE drivers.Drivers SET Status = 'Available'
  │                       │
  │                       ▼  Response: 200 OK
  │
  │   saga.MarkCompensated()   [Status: Compensated]
  │   repository.Save(saga)
  │
  ▼
MyRide.API → return 503 Service Unavailable
  { message: "Ride could not be started. Please try again.", status: "Compensated" }


  If compensation also fails:
  │   saga.MarkCompensationFailed("...")   [Status: CompensationFailed]
  │   repository.Save(saga)
  │     └─► Driver stuck as InProgress — flagged for recovery job
```

---

### Recovery Job (Compensating states)

```
SagaRecoveryJob  [runs every 30 seconds]
  │
  ├─► IStartRideSagaRepository.GetStuck()
  │     └─► SELECT FROM orchestrator.StartRideSagas
  │               WHERE Status IN ('Compensating', 'CompensationFailed')
  │
  └─► foreach stuck saga:
        StartRideSaga.Compensate(saga)
          └─► retries FreeDriver as above
```

---
---

## Flow 2: Complete Ride SAGA

### Entry Point
```
Client  →  POST /api/v1/rides/{rideId}/complete  (MyRide.API)
           Headers: X-Tenant-Id: tenant1
```

---

### Full Sequence

```
Client
  │
  ▼
MyRide.API  RidesController.CompleteRide
  │
  ├─► CompleteRideSaga.Execute(rideId, tenantId)
  │
  │   ── Fetch ride details (pre-SAGA) ─────────────────────────────────────────────────────────
  │
  │   IDownstreamRidesClient.GetRide(rideId, tenantId)
  │     └─► RidesApiClient.GetRide
  │           └─► Refit: IRidesApi.GetRide(rideId, tenantId)
  │                 └─► GET http://localhost:5122/api/v1/rides/{rideId}
  │                       Header: X-Tenant-Id: tenant1
  │                       │
  │                       ▼
  │                  Rides.API  RidesController.GetRide
  │                       └─► GetActiveRidesHandler.GetById(rideId, tenantId)
  │                             └─► SqlRideReadStore.GetById
  │                                   └─► SELECT FROM rides.RideReadModels
  │                                         WHERE RideId = ? AND TenantId = ?
  │                       │
  │                       ▼  Response: 200 OK
  │                 { rideId, tenantId, riderId, driverId, driverName,
  │                   status, fareAmount, fareCurrency, lastUpdatedOn }
  │
  │     maps to RideResult { RideId, RiderId, DriverId, FareAmount, FareCurrency }
  │
  │   saga = CompleteRideSagaState.Create(rideId, driverId, riderId, tenantId, fare...)
  │   repository.Save(saga)
  │     └─► INSERT INTO orchestrator.CompleteRideSagas   [Status: Pending]
  │
  │   ════════════════════════════════════════════════
  │   STEP 1 — Complete Ride
  │   ════════════════════════════════════════════════
  │
  │   IDownstreamRidesClient.CompleteRide(rideId, tenantId)
  │     └─► RidesApiClient.CompleteRide
  │           └─► Refit: IRidesApi.CompleteRide(rideId, tenantId)
  │                 └─► POST http://localhost:5122/api/v1/rides/{rideId}/complete
  │                       Header: X-Tenant-Id: tenant1
  │                       │
  │                       ▼
  │                  Rides.API  RidesController.CompleteRide
  │                       └─► CompleteRideHandler
  │                             ├─► EventStoreDbRideEventStore.Load(rideId, tenantId)
  │                             │     └─► EventStoreDB: READ tenant1-ride-{rideId}
  │                             ├─► ride.Complete()
  │                             │     └─► emits RideCompleted event
  │                             ├─► EventStoreDbRideEventStore.Append(ride)
  │                             │     └─► EventStoreDB: APPEND tenant1-ride-{rideId}
  │                             ├─► NoOpRideEventPublisher.Publish(RideCompleted)
  │                             └─► SqlRideReadStore.Upsert(readModel)
  │                                   └─► UPDATE rides.RideReadModels SET Status = 'Completed'
  │                       │
  │                       ▼  Response: 200 OK
  │
  │   saga.MarkRideCompleted()   [Status: RideCompleted]
  │   repository.Save(saga)
  │     └─► UPDATE orchestrator.CompleteRideSagas SET Status = 'RideCompleted'
  │
  │   ════════════════════════════════════════════════
  │   STEP 2 — Free Driver
  │   ════════════════════════════════════════════════
  │
  │   IDownstreamDriversClient.FreeDriver(driverId, rideId, tenantId)
  │     └─► DriversApiClient.FreeDriver
  │           └─► Refit: IDriversApi.FreeDriver(driverId, body)
  │                 └─► POST http://localhost:5042/api/v1/drivers/{driverId}/free
  │                       Body: { rideId: "...", tenantId: "tenant1" }
  │                       │
  │                       ▼
  │                  Drivers.API  DriversController.Free
  │                       └─► FreeDriverHandler
  │                             ├─► EventStoreDbDriverEventStore.Load(driverId, tenantId)
  │                             ├─► driver.Free(rideId)
  │                             │     └─► emits DriverFreed event
  │                             ├─► EventStoreDbDriverEventStore.Append(driver)
  │                             └─► SqlDriverRepository.UpdateStatus(driverId, Available, tenantId)
  │                                   └─► UPDATE drivers.Drivers SET Status = 'Available'
  │                       │
  │                       ▼  Response: 200 OK
  │
  │   saga.MarkDriverFreed()   [Status: DriverFreed]
  │   repository.Save(saga)
  │     └─► UPDATE orchestrator.CompleteRideSagas SET Status = 'DriverFreed'
  │
  │   ════════════════════════════════════════════════
  │   STEP 3 — Charge Rider
  │   ════════════════════════════════════════════════
  │
  │   IDownstreamPaymentsClient.ChargeRider(riderId, driverId, amount, currency, tenantId)
  │     └─► PaymentsApiClient.ChargeRider
  │           └─► Refit: IPaymentsApi.ChargeRider(body, tenantId)
  │                 └─► POST http://localhost:5001/api/v1/payments/charge
  │                       Header: X-Tenant-Id: tenant1
  │                       Body:   { payerId: riderId, payeeId: driverId,
  │                                 amount, currency, simulateFailure: false }
  │                       │
  │                       ▼
  │                  Payments.API
  │                       └─► creates payment record, charges rider
  │                       │
  │                       ▼  Response: 200 OK
  │                 { paymentId: "...", message: "..." }
  │
  │   saga.MarkPaymentCharged(paymentId)   [Status: PaymentCharged]
  │   repository.Save(saga)
  │     └─► UPDATE orchestrator.CompleteRideSagas
  │               SET Status = 'PaymentCharged', PaymentId = ?
  │
  │   ════════════════════════════════════════════════
  │   STEP 4 — Pay Driver
  │   ════════════════════════════════════════════════
  │
  │   IDownstreamPayoutsClient.PayDriver(driverId, amount, currency, tenantId)
  │     └─► PayoutsApiClient.PayDriver
  │           └─► Refit: IPayoutsApi.PayDriver(body, tenantId)
  │                 └─► POST http://localhost:5203/api/v1/payouts/pay
  │                       Header: X-Tenant-Id: tenant1
  │                       Body:   { recipientId: driverId, amount, currency,
  │                                 simulateFailure: false }
  │                       │
  │                       ▼
  │                  Payouts.API
  │                       └─► creates payout record, pays driver
  │                       │
  │                       ▼  Response: 200 OK
  │
  │   saga.MarkCompleted()   [Status: Completed]
  │   repository.Save(saga)
  │     └─► UPDATE orchestrator.CompleteRideSagas SET Status = 'Completed'
  │
  ▼
MyRide.API → return 200 OK
  { rideId, status: "Completed", message: "Ride completed." }
```

---

### Forward Recovery Paths

```
  STEP 2 fails (FreeDriver):
  │   saga.IncrementRetryCount() if retries < 3
  │   saga.MarkFreeDriverFailed() if retries exhausted
  │     → Recovery job retries Step 2 on next run
  │     → SAGA resumes from Status = 'RideCompleted'

  STEP 3 fails (ChargeRider):
  │   saga.IncrementRetryCount() if retries < 3
  │   saga.MarkPaymentFailed() if retries exhausted
  │     → Recovery job retries Step 3 on next run
  │     → SAGA resumes from Status = 'DriverFreed'
  │     → No rollback. Ride is complete. Payment flagged for review.

  STEP 4 fails (PayDriver):
  │   saga.IncrementRetryCount() if retries < 3
  │   saga.MarkPayoutFailed() if retries exhausted
  │     → Recovery job retries Step 4 on next run
  │     → SAGA resumes from Status = 'PaymentCharged'
  │     → No rollback. Rider already charged. Payout flagged for review.
```

---

### Recovery Job (Forward states)

```
SagaRecoveryJob  [runs every 30 seconds]
  │
  ├─► ICompleteRideSagaRepository.GetStuck()
  │     └─► SELECT FROM orchestrator.CompleteRideSagas
  │               WHERE Status IN ('FreeDriverFailed', 'PaymentFailed', 'PayoutFailed')
  │
  └─► foreach stuck saga:
        CompleteRideSaga.Resume(saga)
          └─► picks up from current Status and retries the failed step
```

---

## Service Ports Quick Reference

| Service | Port | Base URL |
|---------|------|----------|
| MyRide.API | 5239 | http://localhost:5239 |
| Rides.API | 5122 | http://localhost:5122 |
| Drivers.API | 5042 | http://localhost:5042 |
| Payments.API | 5001 | http://localhost:5001 |
| Payouts.API | 5203 | http://localhost:5203 |

## SAGA State Storage

| Table | Schema | Tracks |
|-------|--------|--------|
| StartRideSagas | orchestrator | StartRide SAGA per ride request |
| CompleteRideSagas | orchestrator | CompleteRide SAGA per completion |
