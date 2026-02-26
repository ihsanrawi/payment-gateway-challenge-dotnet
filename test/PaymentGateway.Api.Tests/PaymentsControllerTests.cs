using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Internal;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.External;
using PaymentGateway.Infrastructure.Repository;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();

    private static HttpClient CreateTestClient(Action<Mock<IBankClient>>? configureMock = null)
    {
        var mockBankClient = new Mock<IBankClient>();
        configureMock?.Invoke(mockBankClient);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockBankClient.Object);
                }));
        return webApplicationFactory.CreateClient();
    }

    [Fact]
    public async Task ProcessPayment_InvalidRequest_ReturnsRejectedStatus()
    {
        // Arrange
        var client = CreateTestClient();

        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "1234567890123",
            ExpiryMonth = 13,
            ExpiryYear = 2020,
            Currency = "GBP",
            Amount = 10000,
            Cvv = "12"
        };

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        request.Content = JsonContent.Create(invalidRequest);
        var response = await client.SendAsync(request);

        // Assert - Should return 400 Bad Request with Rejected status
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var jsonDoc = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.NotNull(jsonDoc);
        Assert.Equal("Rejected", jsonDoc.RootElement.GetProperty("status").GetString());
        Assert.True(jsonDoc.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task ProcessPayment_InvalidIdempotencyKey_Returns400BadRequest()
    {
        // Arrange
        var client = CreateTestClient();

        var validRequest = new PostPaymentRequest
        {
            CardNumber = "123456789012345",
            ExpiryMonth = 10,
            ExpiryYear = 2026,
            Currency = "GBP",
            Amount = 10000,
            Cvv = "123"
        };

        // Act - Send whitespace as the Idempotency-Key header (will be trimmed and fail our validation)
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
        request.Headers.Add("Idempotency-Key", "   ");
        request.Content = JsonContent.Create(validRequest);
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

         var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.NotEmpty(problemDetails.Errors);
        Assert.Contains(problemDetails.Errors.Keys, k => k.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcessPayment_ValidRequest_Returns200OK()
    {
        // Arrange
        var client = CreateTestClient(mock =>
            mock.Setup(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BankPaymentResult(
                    Success: true,
                    Authorized: true,
                    AuthorizationCode: "AUTH123",
                    ErrorMessage: null)));

        var validRequest = new PostPaymentRequest
        {
            CardNumber = "123456789012345",
            ExpiryMonth = 10,
            ExpiryYear = 2026,
            Currency = "GBP",
            Amount = 10000,
            Cvv = "123"
        };

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        request.Content = JsonContent.Create(validRequest);
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new Payment(
            Id: Guid.NewGuid(),
            Status: PaymentStatus.Authorized,
            CardNumberLastFour: _random.Next(1111, 9999),
            ExpiryMonth: _random.Next(1, 12),
            ExpiryYear: _random.Next(2023, 2030),
            Currency: "GBP",
            Amount: _random.Next(1, 10000)
        );

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton<IPaymentsRepository>(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_InvalidRequestThenValidRequest_SameIdempotencyKey_RetriesAllowed()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var client = CreateTestClient(mock =>
            mock.Setup(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BankPaymentResult(
                    Success: true,
                    Authorized: true,
                    AuthorizationCode: "AUTH123",
                    ErrorMessage: null)));

        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "123",
            ExpiryMonth = 13,
            ExpiryYear = 2020,
            Currency = "GBP",
            Amount = 10000,
            Cvv = "12"
        };

        var validRequest = new PostPaymentRequest
        {
            CardNumber = "4242424242424241", // Ends with odd number = Authorized
            ExpiryMonth = 10,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "GBP",
            Amount = 10000,
            Cvv = "123"
        };

        // Act - Send first request with invalid data (should be Rejected)
        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
        request1.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
        request1.Content = JsonContent.Create(invalidRequest);
        var response1 = await client.SendAsync(request1);
        var jsonDoc1 = await response1.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();

        // Send second request with valid data using same idempotency key (should be processed)
        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
        request2.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
        request2.Content = JsonContent.Create(validRequest);
        var response2 = await client.SendAsync(request2);
        var payment2 = await response2.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert - First should be Rejected, second should be Authorized
        Assert.NotNull(jsonDoc1);
        Assert.NotNull(payment2);
        Assert.Equal("Rejected", jsonDoc1.RootElement.GetProperty("status").GetString());
        Assert.Equal(PaymentStatus.Authorized, payment2.Status);
    }

    [Fact]
    public async Task ProcessPayment_ValidRequest_SameIdempotencyKey_ReturnsSamePayment()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var client = CreateTestClient(mock =>
            mock.Setup(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BankPaymentResult(
                    Success: true,
                    Authorized: true,
                    AuthorizationCode: "AUTH123",
                    ErrorMessage: null)));

        var validRequest = new PostPaymentRequest
        {
            CardNumber = "4242424242424241", // Ends with odd number = Authorized
            ExpiryMonth = 10,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "GBP",
            Amount = 10000,
            Cvv = "123"
        };

        // Act - Send first request
        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
        request1.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
        request1.Content = JsonContent.Create(validRequest);
        var response1 = await client.SendAsync(request1);
        var payment1 = await response1.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Send second request with same idempotency key
        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
        request2.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
        request2.Content = JsonContent.Create(validRequest);
        var response2 = await client.SendAsync(request2);
        var payment2 = await response2.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert - Both should return same payment ID (idempotency works for valid requests)
        Assert.NotNull(payment1);
        Assert.NotNull(payment2);
        Assert.Equal(payment1.Id, payment2.Id);
        Assert.Equal(PaymentStatus.Authorized, payment1.Status);
        Assert.Equal(PaymentStatus.Authorized, payment2.Status);
    }
}