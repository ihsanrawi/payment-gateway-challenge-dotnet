using PaymentGateway.Domain.Models;

namespace PaymentGateway.Application.Services;

public interface IPaymentProcessorService
{
    /// <summary>
    /// Processes a payment request asynchronously.
    /// </summary>
    Task<PostPaymentResponse> ProcessPaymentAsync(Guid idempotencyKey, PostPaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a previously processed payment asynchronously based on the provided payment ID.
    /// </summary>
    Task<PostPaymentResponse?> RetrieveProcessedPaymentAsync(Guid paymentId);
}