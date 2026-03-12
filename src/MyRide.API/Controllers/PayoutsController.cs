using Microsoft.AspNetCore.Mvc;
using Payouts.Application.Handlers;
using Payouts.Domain.Commands;

namespace MyRide.API.Controllers;

[ApiController]
[Route("api/payouts")]
public class PayoutsController : ControllerBase
{
    private readonly PayDriverHandler payDriverHandler;
    private readonly CancelPayoutHandler cancelPayoutHandler;

    public PayoutsController(PayDriverHandler payDriverHandler, CancelPayoutHandler cancelPayoutHandler)
    {
        this.payDriverHandler = payDriverHandler;
        this.cancelPayoutHandler = cancelPayoutHandler;
    }

    [HttpPost("pay")]
    public async Task<IActionResult> PayDriver([FromBody] PayDriverRequest request, [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new PayDriverCommand(
            Guid.NewGuid(),
            tenantId,
            request.RecipientId,
            request.Amount,
            request.Currency,
            request.SimulateFailure);

        await payDriverHandler.HandleAsync(command);

        return Ok(new { Message = "Driver paid." });
    }

    [HttpPost("{payoutId:guid}/cancel")]
    public async Task<IActionResult> CancelPayout(Guid payoutId, [FromBody] CancelPayoutRequest request, [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new CancelPayoutCommand(payoutId, tenantId, request.Reason);

        await cancelPayoutHandler.HandleAsync(command);

        return Ok(new { payoutId, Message = "Payout cancelled." });
    }
}

public record PayDriverRequest(
    Guid RecipientId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);

public record CancelPayoutRequest(string Reason);
