using Asp.Versioning;
using Drivers.API.Models.Requests;
using Drivers.Application.Handlers;
using Drivers.Application.Queries;
using Drivers.Domain.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Drivers.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/drivers")]
public class DriversController : ControllerBase
{
    private readonly GetAvailableDriverHandler getAvailableDriverHandler;
    private readonly AssignDriverHandler assignDriverHandler;
    private readonly FreeDriverHandler freeDriverHandler;

    public DriversController(
        GetAvailableDriverHandler getAvailableDriverHandler,
        AssignDriverHandler assignDriverHandler,
        FreeDriverHandler freeDriverHandler)
    {
        this.getAvailableDriverHandler = getAvailableDriverHandler;
        this.assignDriverHandler = assignDriverHandler;
        this.freeDriverHandler = freeDriverHandler;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var driver = await getAvailableDriverHandler.Handle(new GetAvailableDriverQuery(tenantId));

        if (driver is null)
        {
            return NotFound(new { Message = "No available drivers at this time." });
        }

        return Ok(driver);
    }

    [HttpPost("{driverId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid driverId, [FromBody] AssignDriverRequest request)
    {
        await assignDriverHandler.Handle(new AssignDriverCommand(driverId, request.RideId, request.TenantId));
        return Ok();
    }

    [HttpPost("{driverId:guid}/free")]
    public async Task<IActionResult> Free(Guid driverId, [FromBody] FreeDriverRequest request)
    {
        await freeDriverHandler.Handle(new FreeDriverCommand(driverId, request.RideId, request.TenantId));
        return Ok();
    }
}
