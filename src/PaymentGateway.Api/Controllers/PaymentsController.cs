using FluentValidation;
using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Application.Services;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(
    ILogger<PaymentsController> logger,
    IPaymentProcessorService paymentProcessorService,
    IValidator<PostPaymentRequest> requestValidator)
    : Controller
{
    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync(PostPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { "IdempotencyKey", ["Idempotency key is required"] }
            }));
        }

        if (!Guid.TryParse(idempotencyKey, out var idempotencyKeyGuid))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { "IdempotencyKey", ["Idempotency key must be a valid GUID"] }
            }));
        }

        var validationResult = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogInformation("Payment validation failed for idempotency key {IdempotencyKey}", idempotencyKeyGuid);
            var rejectedResponse = await paymentProcessorService.ProcessRejectedPaymentAsync(idempotencyKeyGuid, request);

            return Ok(rejectedResponse);
        }

        try
        {
            logger.LogInformation("Processing payment with idempotency key {IdempotencyKey}", idempotencyKeyGuid);

            var response = await paymentProcessorService.ProcessPaymentAsync(idempotencyKeyGuid, request, cancellationToken);

            logger.LogInformation("Payment processed with idempotency key {IdempotencyKey}", idempotencyKeyGuid);
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Payment processing cancelled for idempotency key {IdempotencyKey}", idempotencyKeyGuid);
            return StatusCode(499, "Request cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred processing payment with idempotency key {IdempotencyKey}", idempotencyKeyGuid);
            return Problem("An error occurred processing the payment. Please try again later.");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse>> GetPaymentAsync(Guid id)
    {
        logger.LogInformation("Retrieving payment with ID {PaymentId}", id);
        var payment = await paymentProcessorService.RetrieveProcessedPaymentAsync(id);

        if (payment == null)
        {
            logger.LogInformation("Payment not found with ID {PaymentId}", id);
            return NotFound();
        }

        logger.LogInformation("Payment found with ID {PaymentId}", id);
        return Ok(payment);
    }
}