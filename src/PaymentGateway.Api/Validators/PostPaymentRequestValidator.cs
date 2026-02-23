using System.Data;

using FluentValidation;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest> 
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "GBP", "EUR", "USD"
    };

    public PostPaymentRequestValidator()
    {
        // validated against luhn algorithm and should not be persisted in plaintext
        RuleFor(p => p.CardNumber)
            .NotEmpty()
            .WithMessage("Card number is required")
            .Length(14, 19)
            .WithMessage("Card number must be between 14-19 digits")
            .Must(cardNumber => cardNumber.All(char.IsDigit))
            .WithMessage("Card number must only contain digits");
        
        RuleFor(p => p.ExpiryMonth)
            .InclusiveBetween(1,12)
            .WithMessage("Expiry month must be between 1-12");

        RuleFor(p => p.ExpiryYear)
            .InclusiveBetween(2023, 2030)
            .WithMessage("Expiry year must be between 2023-2030")
            .When(p => p.ExpiryMonth >= 1 && p.ExpiryMonth <= 12);

        RuleFor(p => p.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be 3 characters")
            .Must(currency => SupportedCurrencies.Contains(currency))
            .WithMessage("Currency must be one of: GBP, EUR, USD");
        
        RuleFor(p => p.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(100_000_000) // example: max 1,000,000.00 in minor units
            .WithMessage("Amount is too large.");
        
        RuleFor(p => p.Cvv)
            .NotEmpty()
            .WithMessage("CVV is required")
            .Length(3, 4)
            .WithMessage("CVV must be between 3-4 digits")
            .Must(cardNumber => cardNumber.All(char.IsDigit))
            .WithMessage("CVV must only contain digits");
            
    }
}