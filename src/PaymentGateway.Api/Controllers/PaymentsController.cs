using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repository;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly PaymentsRepository _paymentsRepository;

    public PaymentsController(ILogger<PaymentsController> logger, PaymentsRepository paymentsRepository)
    {
        _logger = logger;
        _paymentsRepository = paymentsRepository;
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync(PostPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { "IdempotencyKey", new[] { "Idempotency key is required" } }
            }));
        }

        // TODO: Check if idempotency key already exists in DB and return 409 Conflict if it does.
        // Return same response as original request if it exists to ensure idempotency.
        return Ok();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}