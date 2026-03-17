using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MyRide.Application.Ports;
using MyRide.Application.Sagas;
using MyRide.Domain.Sagas;
using MyRide.API.Models.Requests;
using MyRide.Infrastructure.Clients.Refit;
using MyRide.Infrastructure.Models;

namespace MyRide.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/rides")]
public class RidesController : ControllerBase
{
    private readonly StartRideSaga startRideSaga;
    private readonly CompleteRideSaga completeRideSaga;
    private readonly IDownstreamDriversClient driversClient;
    private readonly IRidesApi ridesApi;

    public RidesController(
        StartRideSaga startRideSaga,
        CompleteRideSaga completeRideSaga,
        IDownstreamDriversClient driversClient,
        IRidesApi ridesApi)
    {
        this.startRideSaga = startRideSaga;
        this.completeRideSaga = completeRideSaga;
        this.driversClient = driversClient;
        this.ridesApi = ridesApi;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveRides(
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var rides = await ridesApi.GetActiveRides(tenantId);
        return Ok(rides);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartRide(
        [FromBody] RequestRideRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var driver = await driversClient.GetAvailableDriver(tenantId);

        if (driver is null)
        {
            return Conflict(new { Message = "No drivers available. Please try again shortly." });
        }

        var riderId = Guid.NewGuid();

        var saga = await startRideSaga.Execute(
            driver.Id,
            riderId,
            driver.Name,
            tenantId,
            request.FareAmount,
            request.FareCurrency,
            request.PickupLat,
            request.PickupLng,
            request.DropoffLat,
            request.DropoffLng);

        if (saga.Status != StartRideSagaStatus.Completed)
        {
            return StatusCode(503, new { Message = "Ride could not be started. Please try again.", saga.Status });
        }

        return Ok(new
        {
            saga.RideId,
            RiderId = riderId,
            DriverId = driver.Id,
            DriverName = driver.Name,
            Message = "Ride started."
        });
    }

    [HttpPost("{rideId:guid}/accept")]
    public async Task<IActionResult> AcceptRide(
        Guid rideId,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await ridesApi.AcceptRide(rideId, tenantId);
        return Ok(new { rideId, Message = "Ride accepted by driver." });
    }

    [HttpPost("{rideId:guid}/complete")]
    public async Task<IActionResult> CompleteRide(
        Guid rideId,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var saga = await completeRideSaga.Execute(rideId, tenantId);

        if (saga.Status == CompleteRideSagaStatus.Completed)
        {
            return Ok(new { rideId, Message = "Ride completed." });
        }

        return Ok(new { rideId, saga.Status, Message = "Ride completed. Some downstream steps are pending." });
    }

    [HttpPost("{rideId:guid}/cancel")]
    public async Task<IActionResult> CancelRide(
        Guid rideId,
        [FromBody] CancelActionRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var ride = await ridesApi.GetRide(rideId, tenantId);

        await ridesApi.CancelRide(rideId, new CancelRideRequest(request.Reason), tenantId);

        if (ride is not null)
        {
            await driversClient.FreeDriver(ride.DriverId, rideId, tenantId);
        }

        return Ok(new { rideId, Message = "Ride cancelled." });
    }
}
