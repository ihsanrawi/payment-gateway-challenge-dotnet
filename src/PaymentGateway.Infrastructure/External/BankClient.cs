using System.Net;
using System.Net.Http.Json;

using PaymentGateway.Domain.DTOs;
using PaymentGateway.Domain.Internal;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Infrastructure.External;

public class BankClient(HttpClient httpClient) : IBankClient
{
    public async Task<BankPaymentResult> ProcessPaymentAsync(PostPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var bankRequest = new BankRequestDto
        {
            card_number = request.CardNumber,
            expiry_date = $"{request.ExpiryMonth:00}/{request.ExpiryYear:0000}",
            currency = request.Currency,
            amount = request.Amount,
            cvv = request.Cvv
        };

        using var response = await httpClient.PostAsJsonAsync("/payments", bankRequest, cancellationToken);
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            return new BankPaymentResult(false, false, null, "Bank service unavailable");
        }

        response.EnsureSuccessStatusCode();

        var bankResponse = await response.Content.ReadFromJsonAsync<BankResponseDto>(cancellationToken);

        if (bankResponse is null)
        {
            throw new HttpRequestException("Bank service returned empty response");
        }

        return new BankPaymentResult(true, bankResponse.authorized, bankResponse.authorization_code, null);
    }
}