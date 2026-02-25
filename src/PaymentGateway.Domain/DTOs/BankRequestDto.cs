namespace PaymentGateway.Domain.DTOs;

public sealed record BankRequestDto
{
    public string card_number { get; set; } = null!;
    public string expiry_date { get; set; } = null!;
    public string currency { get; set; } = null!;
    public int amount { get; set; }
    public string cvv { get; set; } = null!;
}