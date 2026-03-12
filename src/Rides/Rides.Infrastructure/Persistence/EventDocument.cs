using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Rides.Infrastructure.Persistence;

public class EventDocument
{
    [BsonId]
    public ObjectId MongoId { get; set; }

    public Guid AggregateId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime OccurredOn { get; set; }
}
