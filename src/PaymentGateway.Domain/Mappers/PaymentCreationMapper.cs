using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Domain.Mappers;

public static class PaymentCreationMapper
{
    public static Payment Map(Guid paymentId, PostPaymentRequest request, bool isAuthorized)
    {
        return new Payment(
            paymentId,
            isAuthorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            GetLastFourCardDigits(request.CardNumber),
            request.ExpiryMonth,
            request.ExpiryYear,
            request.Currency,
            request.Amount
        );
    }

    private static int GetLastFourCardDigits(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            return 0;

        return int.Parse(cardNumber[^4..]);
    }
}