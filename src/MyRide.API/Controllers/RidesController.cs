using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MyRide.API.Clients;
using Refit;

namespace MyRide.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/rides")]
public class RidesController : ControllerBase
{
    private readonly IRidesApi ridesApi;
    private readonly IDriversApi driversApi;

    public RidesController(IRidesApi ridesApi, IDriversApi driversApi)
    {
        this.ridesApi = ridesApi;
        this.driversApi = driversApi;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveRides(
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var rides = await ridesApi.GetActiveRidesAsync(tenantId);
        return Ok(rides);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartRide(
        [FromBody] RequestRideRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        AvailableDriverResponse driver;

        try
        {
            driver = await driversApi.GetAvailableDriverAsync(tenantId);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Conflict(new { Message = "No drivers available. Please try again shortly." });
        }

        var riderId = Guid.NewGuid();
        var downstreamRequest = new StartRideRequest(
            riderId,
            driver.Id,
            driver.Name,
            request.FareAmount,
            request.FareCurrency,
            request.PickupLat,
            request.PickupLng,
            request.DropoffLat,
            request.DropoffLng);

        var response = await ridesApi.StartRideAsync(downstreamRequest, tenantId);

        return Ok(new { response.RideId, RiderId = riderId, DriverId = driver.Id, DriverName = driver.Name, response.Message });
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
        [FromBody] CancelActionRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await ridesApi.CancelRideAsync(rideId, new CancelRideRequest(request.Reason), tenantId);
        return Ok(new { rideId, Message = "Ride cancelled." });
    }
}

public record RequestRideRequest(decimal FareAmount, string FareCurrency, double PickupLat, double PickupLng, double DropoffLat, double DropoffLng);
public record CancelActionRequest(string Reason);
