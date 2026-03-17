namespace Drivers.API.Models.Requests;

public record FreeDriverRequest(Guid RideId, string TenantId);
