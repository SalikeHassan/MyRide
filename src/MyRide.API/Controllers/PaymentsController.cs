using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MyRide.API.Clients;

namespace MyRide.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentsApi paymentsApi;

    public PaymentsController(IPaymentsApi paymentsApi)
    {
        this.paymentsApi = paymentsApi;
    }

    [HttpPost("charge")]
    public async Task<IActionResult> ChargeRider(
        [FromBody] ChargeRiderRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        var response = await paymentsApi.ChargeRiderAsync(request, tenantId);
        return Ok(response);
    }

    [HttpPost("{paymentId:guid}/refund")]
    public async Task<IActionResult> RefundRider(
        Guid paymentId,
        [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        await paymentsApi.RefundRiderAsync(paymentId, tenantId);
        return Ok(new { paymentId, Message = "Rider refunded." });
    }
}
