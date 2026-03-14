using Asp.Versioning;
using Drivers.Application.Handlers;
using Drivers.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Drivers.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/drivers")]
public class DriversController : ControllerBase
{
    private readonly GetAvailableDriverHandler getAvailableDriverHandler;

    public DriversController(GetAvailableDriverHandler getAvailableDriverHandler)
    {
        this.getAvailableDriverHandler = getAvailableDriverHandler;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var driver = await getAvailableDriverHandler.HandleAsync(new GetAvailableDriverQuery(tenantId));

        if (driver is null)
        {
            return NotFound(new { Message = "No available drivers at this time." });
        }

        return Ok(driver);
    }
}
