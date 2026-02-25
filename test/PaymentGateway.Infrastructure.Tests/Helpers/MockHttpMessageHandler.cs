using System.Net;
using System.Text.Json;

namespace PaymentGateway.Infrastructure.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage?>> _responseFunc;

    public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage?>> responseFunc)
    {
        _responseFunc = responseFunc;
    }

    public MockHttpMessageHandler(HttpStatusCode statusCode, object? response = null)
    {
        _responseFunc = _ => Task.FromResult(CreateResponse(statusCode, response));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await _responseFunc(request);

        if (response == null)
        {
            throw new HttpRequestException("Service unavailable");
        }

        return response;
    }

    private static HttpResponseMessage? CreateResponse(HttpStatusCode statusCode, object? response)
    {
        var httpResponse = new HttpResponseMessage(statusCode);

        if (response != null)
        {
            httpResponse.Content = new StringContent(JsonSerializer.Serialize(response), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
        }

        return httpResponse;
    }

    public static MockHttpMessageHandler ForAuthorizedResponse(string authorizationCode)
    {
        return new MockHttpMessageHandler(HttpStatusCode.OK, new
        {
            authorized = true,
            authorization_code = authorizationCode
        });
    }

    public static MockHttpMessageHandler ForUnauthorizedResponse()
    {
        return new MockHttpMessageHandler(HttpStatusCode.OK, new
        {
            authorized = false,
            authorization_code = string.Empty
        });
    }

    public static MockHttpMessageHandler ForServiceUnavailable()
    {
        return new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable, new { });
    }

    public static MockHttpMessageHandler ForBadRequest(string errorMessage = "Not all required properties were sent in the request")
    {
        return new MockHttpMessageHandler(HttpStatusCode.BadRequest, new
        {
            error_message = errorMessage
        });
    }
}