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
    /// Handles the processing of a rejected payment request.
    /// </summary>
    Task<PostPaymentResponse> ProcessRejectedPaymentAsync(Guid idempotencyKey, PostPaymentRequest request);

    /// <summary>
    /// Retrieves a previously processed payment asynchronously based on the provided payment ID.
    /// </summary>
    Task<PostPaymentResponse?> RetrieveProcessedPaymentAsync(Guid paymentId);
}