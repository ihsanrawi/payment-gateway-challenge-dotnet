using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repository;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();

    [Fact]
    public async Task ProcessPayment_InvalidRequest_Returns400BadRequest()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

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

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.True(problemDetails.Errors.ContainsKey("ExpiryMonth"));
        Assert.Contains("Expiry month must be between 1-12", problemDetails.Errors["ExpiryMonth"]);
    }

    [Fact]
    public async Task ProcessPayment_InvalidIdempotencyKey_Returns400BadRequest()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

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
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

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
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
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
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}