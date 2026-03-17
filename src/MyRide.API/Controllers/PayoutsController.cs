using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MyRide.Infrastructure.Clients.Refit;
using MyRide.Infrastructure.Models;

namespace MyRide.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/payouts")]
public class PayoutsController : ControllerBase
{
    private readonly IPayoutsApi payoutsApi;

    public PayoutsController(IPayoutsApi payoutsApi)
    {
        this.payoutsApi = payoutsApi;
    }

    [HttpPost("pay")]
    public async Task<IActionResult> PayDriver(
        [FromBody] PayDriverRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await payoutsApi.PayDriver(request, tenantId);
        return Ok(new { Message = "Driver paid." });
    }

    [HttpPost("{payoutId:guid}/cancel")]
    public async Task<IActionResult> CancelPayout(
        Guid payoutId,
        [FromBody] CancelPayoutRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await payoutsApi.CancelPayout(payoutId, request, tenantId);
        return Ok(new { payoutId, Message = "Payout cancelled." });
    }
}
