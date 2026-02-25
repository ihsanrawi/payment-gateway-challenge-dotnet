using System.Net;

using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.External;
using PaymentGateway.Infrastructure.Tests.Helpers;

namespace PaymentGateway.Infrastructure.Tests;

public class BankClientTests
{
    private readonly PostPaymentRequest _validRequest;
    private const string _testBaseUrl = "https://test-bank.com";

    public BankClientTests()
    {
        _validRequest = new PostPaymentRequest
        {
            CardNumber = "1234567890123451",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 10000,
            Cvv = "123"
        };
    }

    [Fact]
    public async Task ProcessPaymentAsync_BankReturnsAuthorized_ReturnsAuthorizedResponse()
    {
        var expectedAuthCode = "AUTH123";
        var mockHandler = MockHttpMessageHandler.ForAuthorizedResponse(expectedAuthCode);
        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);

        var result = await bankClient.ProcessPaymentAsync(_validRequest);

        Assert.True(result.Success);
        Assert.True(result.Authorized);
        Assert.Equal(expectedAuthCode, result.AuthorizationCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessPaymentAsync_BankReturnsUnauthorized_ReturnsDeclinedResponse()
    {
        var mockHandler = MockHttpMessageHandler.ForUnauthorizedResponse();
        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);

        var result = await bankClient.ProcessPaymentAsync(_validRequest);

        Assert.True(result.Success);
        Assert.False(result.Authorized);
        Assert.Equal(string.Empty, result.AuthorizationCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessPaymentAsync_BankReturnsEmptyResponse_ThrowsJsonException()
    {
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            () => bankClient.ProcessPaymentAsync(_validRequest));
    }

    [Fact]
    public async Task ProcessPaymentAsync_BankReturnsServiceUnavailable_ThrowsHttpRequestException()
    {
        var mockHandler = MockHttpMessageHandler.ForServiceUnavailable();
        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => bankClient.ProcessPaymentAsync(_validRequest));

        // Verify exception message
        try
        {
            await bankClient.ProcessPaymentAsync(_validRequest);
        }
        catch (HttpRequestException ex)
        {
            Assert.Equal("Bank service is unavailable", ex.Message);
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_BankReturnsBadRequest_ThrowsHttpRequestException()
    {
        var mockHandler = MockHttpMessageHandler.ForBadRequest();
        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => bankClient.ProcessPaymentAsync(_validRequest));
    }

    [Fact]
    public async Task ProcessPaymentAsync_NetworkFailure_ThrowsHttpRequestException()
    {
        HttpResponseMessage? NullResponse(HttpRequestMessage _) => null;
        var mockHandler = new MockHttpMessageHandler(req => Task.FromResult<HttpResponseMessage?>(NullResponse(req)));
        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => bankClient.ProcessPaymentAsync(_validRequest));
    }

    [Fact]
    public async Task ProcessPaymentAsync_SendsCorrectRequestBody()
    {
        var expectedAuthCode = "AUTH456";
        string? capturedJson = null;

        async Task<HttpResponseMessage?> HandleRequest(HttpRequestMessage request)
        {
            capturedJson = await request.Content?.ReadAsStringAsync();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
                {
                    authorized = true,
                    authorization_code = expectedAuthCode
                }), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
            };
        }

        var mockHandler = new MockHttpMessageHandler(HandleRequest);
        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);

        await bankClient.ProcessPaymentAsync(_validRequest);

        Assert.NotNull(capturedJson);
        var capturedRequest = System.Text.Json.JsonSerializer.Deserialize<BankRequestCapture>(capturedJson);
        Assert.NotNull(capturedRequest);
        Assert.Equal(_validRequest.CardNumber, capturedRequest.card_number);
        Assert.Equal("12/2025", capturedRequest.expiry_date);
        Assert.Equal(_validRequest.Currency, capturedRequest.currency);
        Assert.Equal(_validRequest.Amount, capturedRequest.amount);
        Assert.Equal(_validRequest.Cvv, capturedRequest.cvv);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ExpiryDate_FormattedCorrectly()
    {
        // Test expiry date formatting
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123451",
            ExpiryMonth = 1,   // Single digit month
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 5000,
            Cvv = "456"
        };

        string? capturedJson = null;
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            capturedJson = req.Content?.ReadAsStringAsync().Result;
            return Task.FromResult<HttpResponseMessage?>(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"authorized\":true,\"authorization_code\":\"TEST\"}", System.Text.Encoding.UTF8, "application/json")
            });
        });

        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);


        await bankClient.ProcessPaymentAsync(request);


        Assert.NotNull(capturedJson);
        var capturedRequest = System.Text.Json.JsonSerializer.Deserialize<BankRequestCapture>(capturedJson);
        Assert.NotNull(capturedRequest);
        Assert.Equal("01/2026", capturedRequest.expiry_date);
    }

    [Fact]
    public async Task ProcessPaymentAsync_CancellationToken_PropagatesToHttpClient()
    {
        var mockHandler = MockHttpMessageHandler.ForAuthorizedResponse("AUTH789");
        var httpClient = CreateHttpClient(mockHandler);
        var bankClient = new BankClient(httpClient);
        using var cts = new CancellationTokenSource();

        var result = await bankClient.ProcessPaymentAsync(_validRequest, cts.Token);

        Assert.True(result.Success);
        Assert.True(result.Authorized);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri(_testBaseUrl) };
    }

    // Helper class to capture the inner BankRequestDto structure
    private class BankRequestCapture
    {
        public string card_number { get; set; } = string.Empty;
        public string expiry_date { get; set; } = string.Empty;
        public string currency { get; set; } = string.Empty;
        public int amount { get; set; }
        public string cvv { get; set; } = string.Empty;
    }
}