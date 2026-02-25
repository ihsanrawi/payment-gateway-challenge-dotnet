namespace PaymentGateway.Domain.DTOs;

public sealed record BankResponseDto
{
    public bool authorized { get; set; }
    public string? authorization_code { get; set; }
}