using Common.Domain;
using Rides.Domain.ReadModels;

namespace Rides.Application.Queries;

public record GetActiveRidesQuery(string TenantId) : IQuery<List<RideReadModel>>;
