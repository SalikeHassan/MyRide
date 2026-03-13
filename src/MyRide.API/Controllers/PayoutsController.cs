using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MyRide.API.Clients;

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
        await payoutsApi.PayDriverAsync(request, tenantId);
        return Ok(new { Message = "Driver paid." });
    }

    [HttpPost("{payoutId:guid}/cancel")]
    public async Task<IActionResult> CancelPayout(
        Guid payoutId,
        [FromBody] CancelPayoutRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await payoutsApi.CancelPayoutAsync(payoutId, request, tenantId);
        return Ok(new { payoutId, Message = "Payout cancelled." });
    }
}
