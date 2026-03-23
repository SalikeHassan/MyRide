using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Payouts.API.Models.Requests;
using Payouts.Application.Handlers;
using Payouts.Domain.Commands;

namespace Payouts.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/payouts")]
public class PayoutsController : ControllerBase
{
    private readonly PayDriverHandler payDriverHandler;
    private readonly CancelPayoutHandler cancelPayoutHandler;

    public PayoutsController(
        PayDriverHandler payDriverHandler,
        CancelPayoutHandler cancelPayoutHandler)
    {
        this.payDriverHandler = payDriverHandler;
        this.cancelPayoutHandler = cancelPayoutHandler;
    }

    [HttpPost("pay")]
    public async Task<IActionResult> PayDriver(
        [FromBody] PayDriverRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new PayDriverCommand(
            request.RideId,
            tenantId,
            request.RecipientId,
            request.Amount,
            request.Currency,
            request.SimulateFailure);

        var payoutId = await payDriverHandler.Handle(command);

        return Ok(new { PayoutId = payoutId, Message = "Driver paid." });
    }

    [HttpPost("{payoutId:guid}/cancel")]
    public async Task<IActionResult> CancelPayout(
        Guid payoutId,
        [FromBody] CancelPayoutRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new CancelPayoutCommand(payoutId, tenantId, request.Reason);
        await cancelPayoutHandler.Handle(command);
        return Ok(new { payoutId, Message = "Payout cancelled." });
    }
}
