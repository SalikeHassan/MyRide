namespace Drivers.API.Models.Requests;

public record AssignDriverRequest(Guid RideId, string TenantId);
