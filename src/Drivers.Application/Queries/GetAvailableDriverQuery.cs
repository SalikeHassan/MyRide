using Common.Domain;
using Drivers.Domain.Entities;

namespace Drivers.Application.Queries;

public record GetAvailableDriverQuery(string TenantId) : IQuery<Driver?>;
