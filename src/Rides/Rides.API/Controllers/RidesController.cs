using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Rides.Application.Handlers;
using Rides.Application.Queries;
using Rides.API.Models.Requests;
using Rides.Domain.Commands;

namespace Rides.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/rides")]
public class RidesController : ControllerBase
{
    private readonly StartRideHandler startRideHandler;
    private readonly AcceptRideHandler acceptRideHandler;
    private readonly CompleteRideHandler completeRideHandler;
    private readonly CancelRideHandler cancelRideHandler;
    private readonly GetActiveRidesHandler getActiveRidesHandler;

    public RidesController(
        StartRideHandler startRideHandler,
        AcceptRideHandler acceptRideHandler,
        CompleteRideHandler completeRideHandler,
        CancelRideHandler cancelRideHandler,
        GetActiveRidesHandler getActiveRidesHandler)
    {
        this.startRideHandler = startRideHandler;
        this.acceptRideHandler = acceptRideHandler;
        this.completeRideHandler = completeRideHandler;
        this.cancelRideHandler = cancelRideHandler;
        this.getActiveRidesHandler = getActiveRidesHandler;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveRides(
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var rides = await getActiveRidesHandler.Handle(new GetActiveRidesQuery(tenantId));
        return Ok(rides);
    }

    [HttpGet("{rideId:guid}")]
    public async Task<IActionResult> GetRide(
        Guid rideId,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var ride = await getActiveRidesHandler.GetById(rideId, tenantId);

        if (ride is null)
        {
            return NotFound();
        }

        return Ok(ride);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartRide(
        [FromBody] StartRideRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new StartRideCommand(
            request.RideId,
            tenantId,
            request.RiderId,
            request.DriverId,
            request.DriverName,
            request.FareAmount,
            request.FareCurrency,
            request.PickupLat,
            request.PickupLng,
            request.DropoffLat,
            request.DropoffLng);

        await startRideHandler.Handle(command);

        return Ok(new { command.RideId, Message = "Ride requested." });
    }

    [HttpPost("{rideId:guid}/accept")]
    public async Task<IActionResult> AcceptRide(
        Guid rideId,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new AcceptRideCommand(rideId, tenantId);
        await acceptRideHandler.Handle(command);
        return Ok(new { rideId, Message = "Ride accepted by driver." });
    }

    [HttpPost("{rideId:guid}/complete")]
    public async Task<IActionResult> CompleteRide(
        Guid rideId,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new CompleteRideCommand(rideId, tenantId);
        await completeRideHandler.Handle(command);
        return Ok(new { rideId, Message = "Ride completed." });
    }

    [HttpPost("{rideId:guid}/cancel")]
    public async Task<IActionResult> CancelRide(
        Guid rideId,
        [FromBody] CancelRideRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new CancelRideCommand(rideId, tenantId, request.Reason);
        await cancelRideHandler.Handle(command);
        return Ok(new { rideId, Message = "Ride cancelled." });
    }
}
