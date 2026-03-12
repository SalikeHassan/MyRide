# MyRide — Test Requests

Base URL: `http://localhost:{port}`
Required Header on all requests: `X-Tenant-Id: tenant1`

---

## Rides

### Start a Ride
**POST** `/api/rides/start`
```json
{
  "riderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "driverId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "fareAmount": 25.50,
  "fareCurrency": "GBP",
  "pickupLat": 51.5074,
  "pickupLng": -0.1278,
  "dropoffLat": 51.5155,
  "dropoffLng": -0.0922
}
```
> Copy the `rideId` from the response to use in the next requests.

---

### Complete a Ride
**POST** `/api/rides/{rideId}/complete`

No body required.

---

### Cancel a Ride
**POST** `/api/rides/{rideId}/cancel`
```json
{
  "reason": "Rider requested cancellation"
}
```

---

## Test Scenarios

### Happy Path (Start → Complete)
1. POST `/api/rides/start` → copy `rideId`
2. POST `/api/rides/{rideId}/complete`
3. Check MongoDB `tenant1_events` → should have 2 docs (Version 1: RideStarted, Version 2: RideCompleted)
4. Check MongoDB `tenant1_rides_readmodel` → Status should be `Completed`

---

### Cancellation Path (Start → Cancel)
1. POST `/api/rides/start` → copy `rideId`
2. POST `/api/rides/{rideId}/cancel`
3. Check MongoDB `tenant1_events` → should have 2 docs (Version 1: RideStarted, Version 2: RideCancelled)
4. Check MongoDB `tenant1_rides_readmodel` → Status should be `Cancelled`

---

### Invariant — Rider already has active ride
1. POST `/api/rides/start` → do NOT complete or cancel
2. POST `/api/rides/start` again with the same `riderId`
3. Expected: `400` or `500` with message `Rider already has an active ride`

---

### Invariant — Driver already has active ride
1. POST `/api/rides/start` → do NOT complete or cancel
2. POST `/api/rides/start` again with the same `driverId`
3. Expected: `400` or `500` with message `Driver already has an active ride`

---

### Invariant — Complete an already completed ride
1. POST `/api/rides/start` → copy `rideId`
2. POST `/api/rides/{rideId}/complete`
3. POST `/api/rides/{rideId}/complete` again
4. Expected: error `Ride is already completed`

---

### Invariant — Cancel a completed ride
1. POST `/api/rides/start` → copy `rideId`
2. POST `/api/rides/{rideId}/complete`
3. POST `/api/rides/{rideId}/cancel`
4. Expected: error `Cannot cancel a completed ride`

---

## Test Rider & Driver IDs (reusable)

| Role     | Id                                     |
|----------|----------------------------------------|
| Rider 1  | `3fa85f64-5717-4562-b3fc-2c963f66afa6` |
| Rider 2  | `5fa85f64-5717-4562-b3fc-2c963f66afa8` |
| Driver 1 | `4fa85f64-5717-4562-b3fc-2c963f66afa7` |
| Driver 2 | `6fa85f64-5717-4562-b3fc-2c963f66afa9` |
