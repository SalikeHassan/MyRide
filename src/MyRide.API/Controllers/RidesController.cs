using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MyRide.API.Clients;

namespace MyRide.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/rides")]
public class RidesController : ControllerBase
{
    private readonly IRidesApi ridesApi;

    public RidesController(IRidesApi ridesApi)
    {
        this.ridesApi = ridesApi;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartRide(
        [FromBody] StartRideRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var response = await ridesApi.StartRideAsync(request, tenantId);
        return Ok(response);
    }

    [HttpPost("{rideId:guid}/accept")]
    public async Task<IActionResult> AcceptRide(
        Guid rideId,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await ridesApi.AcceptRideAsync(rideId, tenantId);
        return Ok(new { rideId, Message = "Ride accepted by driver." });
    }

    [HttpPost("{rideId:guid}/complete")]
    public async Task<IActionResult> CompleteRide(
        Guid rideId,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await ridesApi.CompleteRideAsync(rideId, tenantId);
        return Ok(new { rideId, Message = "Ride completed." });
    }

    [HttpPost("{rideId:guid}/cancel")]
    public async Task<IActionResult> CancelRide(
        Guid rideId,
        [FromBody] CancelRideRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await ridesApi.CancelRideAsync(rideId, request, tenantId);
        return Ok(new { rideId, Message = "Ride cancelled." });
    }
}
