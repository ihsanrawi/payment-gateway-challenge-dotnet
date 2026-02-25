using PaymentGateway.Infrastructure.Repository;

namespace PaymentGateway.Infrastructure.Tests;

public class IdempotencyRepositoryTests
{
    [Fact]
    public void GetPaymentId_ThrowsArgumentNullException_WhenIdempotencyKeyIsEmpty()
    {
        var sut = new IdempotencyRepository();
        var ex = Assert.Throws<ArgumentNullException>(() => sut.GetPaymentId(Guid.Empty));
        Assert.Equal("idempotencyKey", ex.ParamName);
    }

    [Fact]
    public void GetPaymentId_ReturnsNull_WhenNoMappingExists()
    {
        var sut = new IdempotencyRepository();
        var result = sut.GetPaymentId(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetPaymentId_ReturnsExistingMapping()
    {
        var sut = new IdempotencyRepository();
        var idempotencyKey = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        sut.StoreMapping(idempotencyKey, paymentId);

        var result = sut.GetPaymentId(idempotencyKey);
        Assert.Equal(paymentId, result);
    }

    [Fact]
    public void StoreMapping_ThrowsArgumentNullException_WhenIdempotencyKeyIsEmpty()
    {
        var sut = new IdempotencyRepository();
        var ex = Assert.Throws<ArgumentNullException>(() => sut.StoreMapping(Guid.Empty, Guid.NewGuid()));
        Assert.Equal("idempotencyKey", ex.ParamName);
    }

    [Fact]
    public void StoreMapping_ThrowsArgumentNullException_WhenPaymentIdIsEmpty()
    {
        var sut = new IdempotencyRepository();
        var ex = Assert.Throws<ArgumentNullException>(() => sut.StoreMapping(Guid.NewGuid(), Guid.Empty));
        Assert.Equal("paymentId", ex.ParamName);
    }

    [Fact]
    public void StoreMapping_StoresMapping()
    {
        var sut = new IdempotencyRepository();
        var idempotencyKey = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        sut.StoreMapping(idempotencyKey, paymentId);

        var result = sut.GetPaymentId(idempotencyKey);
        Assert.Equal(paymentId, result);
    }
}