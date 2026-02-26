using AutoMapper;

using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Domain.Mappers.Converters;

public class PaymentRequestConverter : ITypeConverter<PostPaymentRequest, Payment>
{
    public Payment Convert(PostPaymentRequest source, Payment destination, ResolutionContext context)
    {
        var paymentId = context.Items["PaymentId"] is Guid pid ? pid : Guid.Empty;
        var isAuthorized = context.Items["IsAuthorized"] is bool and true;

        return new Payment(
            paymentId,
            isAuthorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            GetLastFourCardDigits(source.CardNumber),
            source.ExpiryMonth,
            source.ExpiryYear,
            source.Currency,
            source.Amount
        );
    }

    private static int GetLastFourCardDigits(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            return 0;

        return int.Parse(cardNumber[^4..]);
    }
}