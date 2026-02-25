using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Infrastructure.Repository;

namespace PaymentGateway.Infrastructure.Tests;

public class PaymentsRepositoryTests
{
    [Fact]
    public void Get_ThrowsArgumentNullException_WhenIdIsEmpty()
    {
        var sut = new PaymentsRepository();
        var ex = Assert.Throws<ArgumentNullException>(() => sut.Get(Guid.Empty));
        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public void Get_ReturnsNull_WhenPaymentNotFound()
    {
        var sut = new PaymentsRepository();
        var result = sut.Get(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void Get_ReturnsPayment_WhenPaymentExists()
    {
        var sut = new PaymentsRepository();
        var payment = new Payment(
            Id: Guid.NewGuid(),
            Status: PaymentStatus.Authorized,
            CardNumberLastFour: 1234,
            ExpiryMonth: 12,
            ExpiryYear: 2026,
            Currency: "GBP",
            Amount: 10000
        );

        sut.Add(payment);
        var result = sut.Get(payment.Id);

        Assert.NotNull(result);
        Assert.Equal(payment.Id, result.Id);
        Assert.Equal(PaymentStatus.Authorized, result.Status);
    }

    [Fact]
    public void Add_AddsPaymentToRepository()
    {
        var sut = new PaymentsRepository();
        var payment = new Payment(
            Id: Guid.NewGuid(),
            Status: PaymentStatus.Declined,
            CardNumberLastFour: 5678,
            ExpiryMonth: 6,
            ExpiryYear: 2025,
            Currency: "USD",
            Amount: 5000
        );

        sut.Add(payment);
        var result = sut.Get(payment.Id);

        Assert.NotNull(result);
        Assert.Equal(payment.Id, result.Id);
        Assert.Equal(PaymentStatus.Declined, result.Status);
        Assert.Equal(5678, result.CardNumberLastFour);
    }

    [Fact]
    public void Add_MultiplePayments_AllCanBeRetrieved()
    {
        var sut = new PaymentsRepository();
        var payment1 = new Payment(
            Id: Guid.NewGuid(),
            Status: PaymentStatus.Authorized,
            CardNumberLastFour: 1111,
            ExpiryMonth: 12,
            ExpiryYear: 2026,
            Currency: "GBP",
            Amount: 10000
        );

        var payment2 = new Payment(
            Id: Guid.NewGuid(),
            Status: PaymentStatus.Rejected,
            CardNumberLastFour: 2222,
            ExpiryMonth: 6,
            ExpiryYear: 2025,
            Currency: "EUR",
            Amount: 5000
        );

        sut.Add(payment1);
        sut.Add(payment2);

        var result1 = sut.Get(payment1.Id);
        var result2 = sut.Get(payment2.Id);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(payment1.Id, result1.Id);
        Assert.Equal(payment2.Id, result2.Id);
    }
}