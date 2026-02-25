using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

public sealed record Payment(
    Guid Id,
    PaymentStatus Status,
    int CardNumberLastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount);