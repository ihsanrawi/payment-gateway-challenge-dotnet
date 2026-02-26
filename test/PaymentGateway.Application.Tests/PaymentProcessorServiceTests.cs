using AutoMapper;

using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Application.Services;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Internal;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.External;
using PaymentGateway.Infrastructure.Repository;

using Xunit;

namespace PaymentGateway.Application.Tests;

public class PaymentProcessorServiceTests
{
    private readonly Mock<IBankClient> _bankClientMock;
    private readonly Mock<IPaymentsRepository> _paymentsRepositoryMock;
    private readonly Mock<IIdempotencyRepository> _idempotencyRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly PaymentProcessorService _sut;

    public PaymentProcessorServiceTests()
    {
        Mock<ILogger<PaymentProcessorService>> loggerMock = new();
        _bankClientMock = new Mock<IBankClient>();
        _paymentsRepositoryMock = new Mock<IPaymentsRepository>();
        _idempotencyRepositoryMock = new Mock<IIdempotencyRepository>();
        _mapperMock = new Mock<IMapper>();

        _sut = new PaymentProcessorService(
            loggerMock.Object,
            _bankClientMock.Object,
            _paymentsRepositoryMock.Object,
            _idempotencyRepositoryMock.Object,
            _mapperMock.Object
        );
    }

    private static PostPaymentRequest CreateValidRequest() =>
        new()
        {
            CardNumber = "4242424242424242",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "GBP",
            Amount = 10050,
            Cvv = "123"
        };

    [Fact]
    public async Task ProcessPaymentAsync_ExistingIdempotencyKey_ReturnsExistingPayment()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var existingPaymentId = Guid.NewGuid();
        var existingPayment = new Payment(
            existingPaymentId,
            PaymentStatus.Authorized,
            4242,
            12,
            DateTime.Now.Year + 1,
            "GBP",
            10050
        );
        var expectedResponse = new PostPaymentResponse
        {
            Id = existingPaymentId,
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = 4242,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "GBP",
            Amount = 10050
        };

        _idempotencyRepositoryMock
            .Setup(x => x.GetPaymentId(idempotencyKey))
            .Returns(existingPaymentId);

        _paymentsRepositoryMock
            .Setup(x => x.Get(existingPaymentId))
            .Returns(existingPayment);

        _mapperMock
            .Setup(x => x.Map<PostPaymentResponse>(existingPayment))
            .Returns(expectedResponse);

        var request = CreateValidRequest();

        // Act
        var result = await _sut.ProcessPaymentAsync(idempotencyKey, request);

        // Assert
        Assert.Equal(expectedResponse, result);
        _bankClientMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentsRepositoryMock.Verify(x => x.Add(It.IsAny<Payment>()), Times.Never);
        _idempotencyRepositoryMock.Verify(x => x.StoreMapping(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_NewIdempotencyKey_BankAuthorizes_CreatesAuthorizedPayment()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var request = CreateValidRequest();

        _idempotencyRepositoryMock
            .Setup(x => x.GetPaymentId(idempotencyKey))
            .Returns((Guid?)null);

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResult(true, true, "AUTH123", null));

        _paymentsRepositoryMock
            .Setup(x => x.Add(It.IsAny<Payment>()));

        _idempotencyRepositoryMock
            .Setup(x => x.StoreMapping(idempotencyKey, It.IsAny<Guid>()));

        var mappedResponse = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = 4242,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "GBP",
            Amount = 10050
        };

        _mapperMock
            .Setup(x => x.Map<PostPaymentResponse>(It.IsAny<Payment>()))
            .Returns(mappedResponse);

        // Act
        var result = await _sut.ProcessPaymentAsync(idempotencyKey, request);

        // Assert
        _bankClientMock.Verify(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _paymentsRepositoryMock.Verify(x => x.Add(It.Is<Payment>(p => p.Status == PaymentStatus.Authorized)), Times.Once);
        _idempotencyRepositoryMock.Verify(x => x.StoreMapping(idempotencyKey, It.IsAny<Guid>()), Times.Once);
        Assert.Equal(PaymentStatus.Authorized, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_NewIdempotencyKey_BankDeclines_CreatesDeclinedPayment()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var request = CreateValidRequest();

        _idempotencyRepositoryMock
            .Setup(x => x.GetPaymentId(idempotencyKey))
            .Returns((Guid?)null);

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResult(true, false, null, null));

        _paymentsRepositoryMock
            .Setup(x => x.Add(It.IsAny<Payment>()));

        _idempotencyRepositoryMock
            .Setup(x => x.StoreMapping(idempotencyKey, It.IsAny<Guid>()));

        var mappedResponse = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Declined,
            CardNumberLastFour = 4242,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "GBP",
            Amount = 10050
        };

        _mapperMock
            .Setup(x => x.Map<PostPaymentResponse>(It.IsAny<Payment>()))
            .Returns(mappedResponse);

        // Act
        var result = await _sut.ProcessPaymentAsync(idempotencyKey, request);

        // Assert
        _paymentsRepositoryMock.Verify(x => x.Add(It.Is<Payment>(p => p.Status == PaymentStatus.Declined)), Times.Once);
        _idempotencyRepositoryMock.Verify(x => x.StoreMapping(idempotencyKey, It.IsAny<Guid>()), Times.Once);
        Assert.Equal(PaymentStatus.Declined, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_BankServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var request = CreateValidRequest();

        _idempotencyRepositoryMock
            .Setup(x => x.GetPaymentId(idempotencyKey))
            .Returns((Guid?)null);

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResult(false, false, null, "Bank service is unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.ProcessPaymentAsync(idempotencyKey, request));

        // Verify no payment was stored
        _paymentsRepositoryMock.Verify(x => x.Add(It.IsAny<Payment>()), Times.Never);
        _idempotencyRepositoryMock.Verify(x => x.StoreMapping(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_PropagatesCancellationToken()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var request = CreateValidRequest();
        var cancellationToken = new CancellationTokenSource().Token;

        _idempotencyRepositoryMock
            .Setup(x => x.GetPaymentId(idempotencyKey))
            .Returns((Guid?)null);

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(request, cancellationToken))
            .ReturnsAsync(new BankPaymentResult(true, true, "AUTH123", null));

        _paymentsRepositoryMock.Setup(x => x.Add(It.IsAny<Payment>()));
        _idempotencyRepositoryMock.Setup(x => x.StoreMapping(idempotencyKey, It.IsAny<Guid>()));

        _mapperMock
            .Setup(x => x.Map<PostPaymentResponse>(It.IsAny<Payment>()))
            .Returns(new PostPaymentResponse { Id = Guid.NewGuid() });

        // Act
        await _sut.ProcessPaymentAsync(idempotencyKey, request, cancellationToken);

        // Assert
        _bankClientMock.Verify(x => x.ProcessPaymentAsync(request, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task RetrieveProcessedPaymentAsync_PaymentFound_ReturnsMappedResponse()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = new Payment(
            paymentId,
            PaymentStatus.Authorized,
            1234,
            6,
            DateTime.Now.Year + 1,
            "USD",
            5000
        );
        var expectedResponse = new PostPaymentResponse
        {
            Id = paymentId,
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = 1234,
            ExpiryMonth = 6,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "USD",
            Amount = 5000
        };

        _paymentsRepositoryMock
            .Setup(x => x.Get(paymentId))
            .Returns(payment);

        _mapperMock
            .Setup(x => x.Map<PostPaymentResponse>(payment))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.RetrieveProcessedPaymentAsync(paymentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task RetrieveProcessedPaymentAsync_PaymentNotFound_ReturnsNull()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _paymentsRepositoryMock
            .Setup(x => x.Get(paymentId))
            .Returns((Payment?)null);

        // Act
        var result = await _sut.RetrieveProcessedPaymentAsync(paymentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]

    public async Task ProcessPaymentAsync_IdempotencyKeyExistsButPaymentNotFound_ProcessesNewPayment()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var existingPaymentId = Guid.NewGuid();
        var request = CreateValidRequest();

        _idempotencyRepositoryMock
            .Setup(x => x.GetPaymentId(idempotencyKey))
            .Returns(existingPaymentId);

        _paymentsRepositoryMock
            .Setup(x => x.Get(existingPaymentId))
            .Returns((Payment?)null);

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResult(true, true, "AUTH456", null));

        _paymentsRepositoryMock.Setup(x => x.Add(It.IsAny<Payment>()));
        _idempotencyRepositoryMock.Setup(x => x.StoreMapping(idempotencyKey, It.IsAny<Guid>()));

        _mapperMock
            .Setup(x => x.Map<PostPaymentResponse>(It.IsAny<Payment>()))
            .Returns(new PostPaymentResponse { Id = Guid.NewGuid() });

        // Act
        await _sut.ProcessPaymentAsync(idempotencyKey, request);

        // Assert
        _bankClientMock.Verify(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _paymentsRepositoryMock.Verify(x => x.Add(It.IsAny<Payment>()), Times.Once);
    }
}