using System.Text.Json;
using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tests;

public class TokenResponseTests
{
    [Fact]
    public void TokenResponse_ShouldInitializeWithDefaultValues()
    {
        // Act
        var tokenResponse = new TokenResponse();

        // Assert
        Assert.Equal(string.Empty, tokenResponse.AccessToken);
        Assert.Equal(string.Empty, tokenResponse.TokenType);
        Assert.Equal(0, tokenResponse.ExpiresIn);
    }

    [Fact]
    public void TokenResponse_ShouldAllowPropertyAssignment()
    {
        // Arrange
        const string expectedAccessToken = "test-access-token";
        const string expectedTokenType = "Bearer";
        const int expectedExpiresIn = 3600;

        // Act
        var tokenResponse = new TokenResponse
        {
            AccessToken = expectedAccessToken,
            TokenType = expectedTokenType,
            ExpiresIn = expectedExpiresIn
        };

        // Assert
        Assert.Equal(expectedAccessToken, tokenResponse.AccessToken);
        Assert.Equal(expectedTokenType, tokenResponse.TokenType);
        Assert.Equal(expectedExpiresIn, tokenResponse.ExpiresIn);
    }

    [Theory]
    [InlineData("access_token", "token_type", "expires_in")]
    [InlineData("ACCESS_TOKEN", "TOKEN_TYPE", "EXPIRES_IN")]
    [InlineData("Access_Token", "Token_Type", "Expires_In")]
    public void TokenResponse_ShouldDeserializeFromJson_WithDifferentCasing(string accessTokenProp, string tokenTypeProp, string expiresInProp)
    {
        // Arrange
        var json = $$"""
                     {
                         "{{accessTokenProp}}": "test-token",
                         "{{tokenTypeProp}}": "Bearer",
                         "{{expiresInProp}}": 7200
                     }
                     """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<TokenResponse>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(7200, result.ExpiresIn);
    }

    [Fact]
    public void TokenResponse_ShouldSerializeToJson_WithCorrectPropertyNames()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            AccessToken = "test-access-token",
            TokenType = "Bearer",
            ExpiresIn = 3600
        };

        // Act
        var json = JsonSerializer.Serialize(tokenResponse);
        var deserializedBack = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert - Check that properties are serialized (they use the C# property names by default)
        Assert.True(deserializedBack.TryGetProperty("AccessToken", out var accessTokenProp) ||
                    deserializedBack.TryGetProperty("access_token", out accessTokenProp));
        Assert.Equal("test-access-token", accessTokenProp.GetString());

        Assert.True(deserializedBack.TryGetProperty("TokenType", out var tokenTypeProp) ||
                    deserializedBack.TryGetProperty("token_type", out tokenTypeProp));
        Assert.Equal("Bearer", tokenTypeProp.GetString());

        Assert.True(deserializedBack.TryGetProperty("ExpiresIn", out var expiresInProp) ||
                    deserializedBack.TryGetProperty("expires_in", out expiresInProp));
        Assert.Equal(3600, expiresInProp.GetInt32());
    }

    [Fact]
    public void TokenResponse_ShouldDeserializeFromJson_WithSnakeCasePropertyNames()
    {
        // Arrange
        var json = """
                   {
                       "access_token": "snake-case-token",
                       "token_type": "Bearer",
                       "expires_in": 1800
                   }
                   """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<TokenResponse>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("snake-case-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(1800, result.ExpiresIn);
    }

    [Fact]
    public void TokenResponse_ShouldHandlePartialJson()
    {
        // Arrange
        var json = """
                   {
                       "access_token": "partial-token"
                   }
                   """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<TokenResponse>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("partial-token", result.AccessToken);
        Assert.Equal(string.Empty, result.TokenType); // Default value
        Assert.Equal(0, result.ExpiresIn); // Default value
    }

    [Fact]
    public void TokenResponse_ShouldHandleNullValues()
    {
        // Arrange
        var json = """
                   {
                       "access_token": null,
                       "token_type": null
                   }
                   """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<TokenResponse>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.AccessToken);
        Assert.Null(result.TokenType);
        Assert.Equal(0, result.ExpiresIn); // Default value when property is missing
    }
}