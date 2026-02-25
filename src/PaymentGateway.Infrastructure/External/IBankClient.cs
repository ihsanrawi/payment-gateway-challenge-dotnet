using PaymentGateway.Domain.Internal;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Infrastructure.External;

public interface IBankClient
{
    /// <summary>
    /// Calls the external bank API to process the payment and returns the result.
    /// </summary>
    Task<BankPaymentResult> ProcessPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken = default);
}