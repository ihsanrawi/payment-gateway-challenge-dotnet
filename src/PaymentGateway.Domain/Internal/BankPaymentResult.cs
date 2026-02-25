namespace PaymentGateway.Domain.Internal;

public sealed record BankPaymentResult(
    bool Success,
    bool Authorized,
    string? AuthorizationCode,
    string? ErrorMessage
);