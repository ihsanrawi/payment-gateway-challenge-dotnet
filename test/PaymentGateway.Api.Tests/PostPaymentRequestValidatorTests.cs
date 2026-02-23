using FluentValidation.Results;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Tests
{
    public class PostPaymentRequestValidatorTests
    {
        private readonly PostPaymentRequestValidator _validator = new PostPaymentRequestValidator();

        private PostPaymentRequest CreateValidRequest() =>
            new PostPaymentRequest
            {
                CardNumber = "4242424242424242",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "GBP",
                Amount = 10050,
                Cvv = "123"
            };

        [Fact]
        public void ValidRequest_ShouldNotHaveValidationErrors()
        {
            var req = CreateValidRequest();
            ValidationResult result = _validator.Validate(req);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void CardNumber_Empty_ShouldHaveRequiredError()
        {
            var req = CreateValidRequest();
            req.CardNumber = string.Empty;

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.CardNumber) && e.ErrorMessage == "Card number is required");
        }

        [Fact]
        public void CardNumber_NonDigit_ShouldHaveDigitsError()
        {
            var req = CreateValidRequest();
            req.CardNumber = "4242x24242424242";

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.CardNumber) && e.ErrorMessage == "Card number must only contain digits");
        }

        [Fact]
        public void CardNumber_InvalidLength_ShouldHaveLengthError()
        {
            var req = CreateValidRequest();
            req.CardNumber = "1234567890"; // too short

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.CardNumber) && e.ErrorMessage == "Card number must be between 14-19 digits");
        }

        [Fact]
        public void ExpiryMonth_OutOfRange_ShouldHaveError()
        {
            var req = CreateValidRequest();
            req.ExpiryMonth = 13;

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.ExpiryMonth) && e.ErrorMessage == "Expiry month must be between 1-12");
        }

        [Fact]
        public void ExpiryYear_OutOfRange_WhenMonthValid_ShouldHaveError()
        {
            var req = CreateValidRequest();
            req.ExpiryMonth = 6; // valid month to trigger year check
            req.ExpiryYear = 2035; // out of allowed range

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.ExpiryYear) && e.ErrorMessage == "Expiry year must be between 2023-2030");
        }

        [Fact]
        public void Currency_Empty_ShouldHaveRequiredError()
        {
            var req = CreateValidRequest();
            req.Currency = string.Empty;

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.Currency) && e.ErrorMessage == "Currency is required");
        }

        [Fact]
        public void Currency_Invalid_ShouldHaveSupportedCurrenciesError()
        {
            var req = CreateValidRequest();
            req.Currency = "JPY";

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.Currency) && e.ErrorMessage == "Currency must be one of: GBP, EUR, USD");
        }

        [Fact]
        public void Amount_Zero_ShouldHaveGreaterThanZeroError()
        {
            var req = CreateValidRequest();
            req.Amount = 0;

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.Amount) && e.ErrorMessage == "Amount must be greater than zero");
        }

        [Fact]
        public void Cvv_Empty_ShouldHaveRequiredError()
        {
            var req = CreateValidRequest();
            req.Cvv = string.Empty;

            var result = _validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.Cvv) && e.ErrorMessage == "CVV is required");
        }

        [Fact]
        public void Cvv_NonDigitOrInvalidLength_ShouldHaveError()
        {
            var req = CreateValidRequest();
            req.Cvv = "12a"; // contains non-digit

            var result = _validator.Validate(req);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.Cvv) && e.ErrorMessage == "CVV must only contain digits");

            req.Cvv = "12"; // too short
            result = _validator.Validate(req);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.Cvv) && e.ErrorMessage == "CVV must be between 3-4 digits");
        }
    }
}
