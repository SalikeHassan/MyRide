using MongoDB.Driver;
using Rides.Application.Ports;
using Rides.Domain.Aggregates;

namespace Rides.Infrastructure.Persistence;

public class MongoRideReadStore : IRideReadStore
{
    private readonly IMongoDatabase database;

    public MongoRideReadStore(IMongoDatabase database)
    {
        this.database = database;
    }

    public async Task<bool> HasActiveRideForRiderAsync(Guid riderId, string tenantId)
    {
        var collection = GetCollection(tenantId);

        var activeStatuses = new[] { RideStatus.Requested, RideStatus.InProgress };

        var filter = Builders<RideReadModel>.Filter.And(
            Builders<RideReadModel>.Filter.Eq(r => r.RiderId, riderId),
            Builders<RideReadModel>.Filter.In(r => r.Status, activeStatuses));

        var count = await collection.CountDocumentsAsync(filter);

        return count > 0;
    }

    public async Task<bool> HasActiveRideForDriverAsync(Guid driverId, string tenantId)
    {
        var collection = GetCollection(tenantId);

        var activeStatuses = new[] { RideStatus.Requested, RideStatus.InProgress };

        var filter = Builders<RideReadModel>.Filter.And(
            Builders<RideReadModel>.Filter.Eq(r => r.DriverId, driverId),
            Builders<RideReadModel>.Filter.In(r => r.Status, activeStatuses));

        var count = await collection.CountDocumentsAsync(filter);

        return count > 0;
    }

    public async Task UpsertAsync(RideReadModel readModel)
    {
        var collection = GetCollection(readModel.TenantId);

        var filter = Builders<RideReadModel>.Filter.Eq(r => r.RideId, readModel.RideId);

        var options = new ReplaceOptions { IsUpsert = true };

        await collection.ReplaceOneAsync(filter, readModel, options);
    }

    private IMongoCollection<RideReadModel> GetCollection(string tenantId)
    {
        return database.GetCollection<RideReadModel>($"{tenantId}_rides_readmodel");
    }
}
