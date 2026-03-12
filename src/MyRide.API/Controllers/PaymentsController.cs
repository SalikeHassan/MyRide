using Microsoft.AspNetCore.Mvc;
using Payments.Application.Handlers;
using Payments.Domain.Commands;

namespace MyRide.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly ChargeRiderHandler chargeRiderHandler;
    private readonly RefundRiderHandler refundRiderHandler;

    public PaymentsController(ChargeRiderHandler chargeRiderHandler, RefundRiderHandler refundRiderHandler)
    {
        this.chargeRiderHandler = chargeRiderHandler;
        this.refundRiderHandler = refundRiderHandler;
    }

    [HttpPost("charge")]
    public async Task<IActionResult> ChargeRider([FromBody] ChargeRiderRequest request, [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new ChargeRiderCommand(
            Guid.NewGuid(),
            tenantId,
            request.PayerId,
            request.PayeeId,
            request.Amount,
            request.Currency,
            request.SimulateFailure);

        await chargeRiderHandler.HandleAsync(command);

        return Ok(new { Message = "Rider charged." });
    }

    [HttpPost("{paymentId:guid}/refund")]
    public async Task<IActionResult> RefundRider(Guid paymentId, [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var command = new RefundRiderCommand(paymentId, tenantId);

        await refundRiderHandler.HandleAsync(command);

        return Ok(new { paymentId, Message = "Rider refunded." });
    }
}

public record ChargeRiderRequest(
    Guid PayerId,
    Guid PayeeId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);
