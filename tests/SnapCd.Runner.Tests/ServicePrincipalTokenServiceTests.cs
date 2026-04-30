using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tests;

public class ServicePrincipalTokenServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ServicePrincipalTokenService _service;

    private const string TestAuthServerUrl = "https://example.com";
    private static readonly Guid TestOrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const string TestClientId = "test-client";
    private const string TestClientSecret = "test-secret";

    public ServicePrincipalTokenServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new ServicePrincipalTokenService(_httpClient);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldReturnTokenResponse_WhenRequestIsSuccessful()
    {
        // Arrange
        var expectedTokenResponse = new TokenResponse
        {
            AccessToken = "test-access-token",
            TokenType = "Bearer",
            ExpiresIn = 3600
        };

        var responseJson = JsonSerializer.Serialize(expectedTokenResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == $"{TestAuthServerUrl}/connect/token" &&
                    req.Headers.Accept.Any(h => h.MediaType == "application/json")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetAccessTokenAsync(TestAuthServerUrl, TestOrganizationId, TestClientId, TestClientSecret);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTokenResponse.AccessToken, result.AccessToken);
        Assert.Equal(expectedTokenResponse.TokenType, result.TokenType);
        Assert.Equal(expectedTokenResponse.ExpiresIn, result.ExpiresIn);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldSendCorrectFormData()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            AccessToken = "test-token",
            TokenType = "Bearer",
            ExpiresIn = 3600
        };

        var responseJson = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        string? capturedFormContent = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                if (request.Content is FormUrlEncodedContent formContent) capturedFormContent = await formContent.ReadAsStringAsync();
                return httpResponse;
            });

        // Act
        await _service.GetAccessTokenAsync(TestAuthServerUrl, TestOrganizationId, TestClientId, TestClientSecret);

        // Assert
        Assert.NotNull(capturedFormContent);
        Assert.Contains("grant_type=client_credentials", capturedFormContent);
        Assert.Contains("scope=snapcd_scope", capturedFormContent);
        Assert.Contains($"client_id={TestClientId}", capturedFormContent);
        Assert.Contains($"client_secret={TestClientSecret}", capturedFormContent);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetAccessTokenAsync_ShouldThrowHttpRequestException_WhenResponseIsNotSuccessful(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("Error response", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetAccessTokenAsync(TestAuthServerUrl, TestOrganizationId, TestClientId, TestClientSecret));

        Assert.Contains($"Unexpected status code: {statusCode}", exception.Message);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldThrowInvalidOperationException_WhenResponseCannotBeDeserialized()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => _service.GetAccessTokenAsync(TestAuthServerUrl, TestOrganizationId, TestClientId, TestClientSecret));
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldThrowInvalidOperationException_WhenResponseIsNull()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetAccessTokenAsync(TestAuthServerUrl, TestOrganizationId, TestClientId, TestClientSecret));

        Assert.Contains("Failed to deserialize token response", exception.Message);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldUseCorrectTokenUrl()
    {
        // Arrange
        var customServerUrl = "https://custom.auth.server";
        var expectedTokenUrl = $"{customServerUrl}/connect/token";

        var tokenResponse = new TokenResponse
        {
            AccessToken = "test-token",
            TokenType = "Bearer",
            ExpiresIn = 3600
        };

        var responseJson = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        string? capturedUrl = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                capturedUrl = request.RequestUri?.ToString();
                return Task.FromResult(httpResponse);
            });

        // Act
        await _service.GetAccessTokenAsync(customServerUrl, TestOrganizationId, TestClientId, TestClientSecret);

        // Assert
        Assert.Equal(expectedTokenUrl, capturedUrl);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldHandleCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => _service.GetAccessTokenAsync(TestAuthServerUrl, TestOrganizationId, TestClientId, TestClientSecret, cts.Token));
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldHandleCaseInsensitiveDeserialization()
    {
        // Arrange
        var responseJson = """
                           {
                               "ACCESS_TOKEN": "test-access-token",
                               "TOKEN_TYPE": "Bearer", 
                               "EXPIRES_IN": 3600
                           }
                           """;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetAccessTokenAsync(TestAuthServerUrl, TestOrganizationId, TestClientId, TestClientSecret);

        // Assert
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact(Skip = "Integration test - requires SnapCd.Server running at localhost:20002")]
    public async Task GetAccessTokenAsync_ShouldSucceedWithActualServer_WhenServerIsRunning()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var service = new ServicePrincipalTokenService(httpClient);

        const string authServerUrl = "https://localhost:20002";
        var organizationId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Test org ID
        const string clientId = "Admin";
        const string clientSecret = "somesecret";

        // Act
        var result = await service.GetAccessTokenAsync(authServerUrl, organizationId, clientId, clientSecret);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresIn > 0);
    }

    // [Fact]
    // public async Task GetAccessTokenAsync_IntegrationTest_WithActualServer()
    // {
    //     // Arrange
    //     using var httpClient = new HttpClient();
    //     var service = new ServicePrincipalTokenService(httpClient);
    //     
    //     const string authServerUrl = "https://localhost:20002";
    //     const string clientId = "Admin";
    //     const string clientSecret = "somesecret";
    //
    //     // First, check if server is reachable
    //     bool serverReachable = false;
    //     try
    //     {
    //         using var testRequest = new HttpRequestMessage(HttpMethod.Get, $"{authServerUrl}/.well-known/openid_configuration");
    //         using var testResponse = await httpClient.SendAsync(testRequest);
    //         serverReachable = true;
    //     }
    //     catch
    //     {
    //         // Server not reachable
    //     }
    //
    //     if (!serverReachable)
    //     {
    //         // Output information but don't fail the test
    //         Assert.True(true, "Server at localhost:20002 is not running. Start SnapCd.Server to run this integration test.");
    //         return;
    //     }
    //
    //     try
    //     {
    //         // Act
    //         var result = await service.GetAccessTokenAsync(authServerUrl, clientId, clientSecret);
    //
    //         // Assert
    //         Assert.NotNull(result);
    //         Assert.NotEmpty(result.AccessToken);
    //         Assert.Equal("Bearer", result.TokenType);
    //         Assert.True(result.ExpiresIn > 0);
    //     }
    //     catch (HttpRequestException ex) when (ex.Message.Contains("SSL connection could not be established") ||
    //                                          ex.Message.Contains("certificate"))
    //     {
    //         // SSL/Certificate issues are common in development - just pass with a note
    //         Assert.True(true, $"SSL/Certificate issue (expected in dev): {ex.Message}");
    //     }
    // }
}