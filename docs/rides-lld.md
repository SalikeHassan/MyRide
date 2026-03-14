# Rides Bounded Context — Low Level Design

## Purpose
Core domain. Manages the lifecycle of a ride from start to completion or cancellation.
Fires the events that trigger the SAGA orchestration.

---

## Ubiquitous Language

| Term     | Meaning                                      |
|----------|----------------------------------------------|
| Rider    | The person requesting the ride               |
| Driver   | The person completing the ride               |
| Trip     | The ride itself (a single journey)           |
| Fare     | The monetary amount charged for the trip     |

---

## Project Structure

```
Rides/
├── Rides.Domain/
│   ├── Aggregates/
│   │   └── RideAggregate.cs
│   ├── Commands/
│   │   ├── StartRideCommand.cs
│   │   ├── CompleteRideCommand.cs
│   │   └── CancelRideCommand.cs
│   ├── Events/
│   │   ├── RideStarted.cs
│   │   ├── RideCompleted.cs
│   │   └── RideCancelled.cs
│   └── ValueObjects/
│       ├── Fare.cs
│       └── Location.cs
├── Rides.Application/
│   └── Handlers/
│       ├── StartRideHandler.cs
│       ├── CompleteRideHandler.cs
│       └── CancelRideHandler.cs
└── Rides.Infrastructure/
    ├── Persistence/
    │   └── MongoRideEventStore.cs
    └── Messaging/
        └── RideEventPublisher.cs
```

---

## Aggregate — RideAggregate

### State
| Property    | Type        | Description                              |
|-------------|-------------|------------------------------------------|
| Id          | Guid        | Unique ride identifier                   |
| TenantId    | string      | Tenant this ride belongs to              |
| RiderId     | Guid        | Who requested the ride                   |
| DriverId    | Guid        | Who is driving                           |
| Fare        | Fare        | Value object — amount + currency         |
| PickupLocation  | Location | Value object — lat/lng               |
| DropoffLocation | Location | Value object — lat/lng               |
| Status      | RideStatus  | Enum: Started, Completed, Cancelled      |
| Version     | int         | Inherited from AggregateRoot             |

### RideStatus Enum
```
Started
Completed
Cancelled
```

### Invariants (rules enforced by the aggregate)
| Scenario                              | Result                        |
|---------------------------------------|-------------------------------|
| CompleteRide on a Cancelled ride      | Throw InvalidOperationException |
| CancelRide on a Completed ride        | Throw InvalidOperationException |
| CompleteRide on an already Completed ride | Throw InvalidOperationException |
| StartRide with no RiderId or DriverId | Throw ArgumentException       |
| StartRide with zero or negative Fare  | Throw ArgumentException       |

---

## Commands

### StartRideCommand
| Property        | Type   |
|-----------------|--------|
| RideId          | Guid   |
| TenantId        | string |
| RiderId         | Guid   |
| DriverId        | Guid   |
| FareAmount      | decimal|
| FareCurrency    | string |
| PickupLat       | double |
| PickupLng       | double |
| DropoffLat      | double |
| DropoffLng      | double |

### CompleteRideCommand
| Property  | Type   |
|-----------|--------|
| RideId    | Guid   |
| TenantId  | string |

### CancelRideCommand
| Property  | Type   |
|-----------|--------|
| RideId    | Guid   |
| TenantId  | string |
| Reason    | string |

---

## Domain Events

All events implement `IDomainEvent` from SharedKernel (Id, OccurredOn, TenantId).

### RideStarted
| Property        | Type    |
|-----------------|---------|
| RideId          | Guid    |
| RiderId         | Guid    |
| DriverId        | Guid    |
| FareAmount      | decimal |
| FareCurrency    | string  |
| PickupLat       | double  |
| PickupLng       | double  |
| DropoffLat      | double  |
| DropoffLng      | double  |

### RideCompleted
| Property    | Type    |
|-------------|---------|
| RideId      | Guid    |
| RiderId     | Guid    |
| DriverId    | Guid    |
| FareAmount  | decimal |
| FareCurrency| string  |

### RideCancelled
| Property  | Type   |
|-----------|--------|
| RideId    | Guid   |
| RiderId   | Guid   |
| Reason    | string |

---

## Value Objects

### Fare
- Properties: `Amount (decimal)`, `Currency (string)`
- Invariant: Amount must be greater than zero
- Equality: by Amount + Currency

### Location
- Properties: `Latitude (double)`, `Longitude (double)`
- Equality: by Latitude + Longitude

---

## Application Handlers

### StartRideHandler
1. Create new `RideAggregate` via factory method
2. Raise `RideStarted` event
3. Persist events to MongoDB event store
4. Publish `RideStarted` to Service Bus

### CompleteRideHandler
1. Load `RideAggregate` by replaying events from MongoDB
2. Call `Complete()` on aggregate (enforces invariants)
3. Raise `RideCompleted` event
4. Persist new event to MongoDB
5. Publish `RideCompleted` to Service Bus → this triggers the SAGA

### CancelRideHandler
1. Load `RideAggregate` by replaying events from MongoDB
2. Call `Cancel(reason)` on aggregate (enforces invariants)
3. Raise `RideCancelled` event
4. Persist new event to MongoDB
5. Publish `RideCancelled` to Service Bus

---

## Infrastructure

### MongoRideEventStore
- Collection: `{tenantId}_events`
- Append-only — never update or delete
- Each document: `{ AggregateId, EventType, EventData (JSON), Version, OccurredOn }`
- Load by: filter on `AggregateId` + `TenantId`, order by `Version` ascending
- Optimistic concurrency: check expected version before append

### RideEventPublisher
- Publishes to Azure Service Bus topic: `rides`
- Subscribers: SAGA (listens for `RideCompleted`)
- Message includes: event type + full event payload as JSON

---

## Event Flow

```
POST /api/rides/start
    → StartRideHandler
        → RideAggregate.Start()
            → RideStarted raised
        → MongoRideEventStore.Append()
        → RideEventPublisher.Publish(RideStarted)

POST /api/rides/{id}/complete
    → CompleteRideHandler
        → MongoRideEventStore.Load() → replay → RideAggregate rebuilt
        → RideAggregate.Complete()
            → RideCompleted raised
        → MongoRideEventStore.Append()
        → RideEventPublisher.Publish(RideCompleted)
            → SAGA picks up RideCompleted → triggers ChargeRider
```

---

## API Endpoints (in MyRide.API)

| Method | Route                        | Handler               |
|--------|------------------------------|-----------------------|
| POST   | /api/rides/start             | StartRideHandler      |
| POST   | /api/rides/{id}/complete     | CompleteRideHandler   |
| POST   | /api/rides/{id}/cancel       | CancelRideHandler     |

---

## Dependencies
- SharedKernel (AggregateRoot, ValueObject, IDomainEvent)
- MongoDB.Driver (Infrastructure only)
- Azure.Messaging.ServiceBus (Infrastructure only)

## Docker commands
Mongo db
  docker run -d --name myride-mongo -p 27017:27017 mongo:7

  Now run the two containers in order:

  1. Azure SQL Edge (backing store for Service Bus)                                                     
  docker run -d \
    --name myride-sqledge \                                                                             
    -e ACCEPT_EULA=Y \                                      
    -e MSSQL_SA_PASSWORD=MyRide@123 \
    -p 1433:1433 \
    mcr.microsoft.com/azure-sql-edge:latest

  2. Azure Service Bus Emulator
 docker run -d --name myride-servicebus -p 5672:5672 -e ACCEPT_EULA=Y -e SQL_SERVER=host.docker.internal -e MSSQL_SA_PASSWORD=MyRide@123 -v /Users/salikehassan/Projects/MultiTenantApp/MyRide/servicebus/config.json:/ServiceBus_Emulator/ConfigFiles/Config.json mcr.microsoft.com/azure-messaging/servicebus-emulator:latest 

docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=MyRide@2024!' -p 1433:1433 --name myride-sql -d mcr.microsoft.com/azure-sql-edge