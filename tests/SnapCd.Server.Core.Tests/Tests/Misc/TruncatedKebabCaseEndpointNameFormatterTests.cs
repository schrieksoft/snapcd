using SnapCd.Server.Core.Misc.Utils;

namespace SnapCd.Server.Core.Tests.Tests.Misc;

public class TruncatedKebabCaseEndpointNameFormatterTests
{
    [Fact]
    public void SanitizeName_WhenNameIsUnder80Characters_ReturnsOriginalName()
    {
        // Arrange
        var formatter = new TruncatedKebabCaseEndpointNameFormatter();
        var shortName = "short-queue-name";

        // Act
        var result = formatter.SanitizeName(shortName);

        // Assert
        Assert.Equal(shortName, result);
    }

    [Fact]
    public void SanitizeName_WhenNameIsOver80Characters_WithSqsLimit_TruncatesAndAddsHash()
    {
        // Arrange
        var formatter = new TruncatedKebabCaseEndpointNameFormatter(maxQueueNameLength: 80);
        var longName = "default-module-approval-threshold-modified-fanout-11a97cdbbfc7434195492a3655fb3358-very-long-additional-suffix";

        // Act
        var result = formatter.SanitizeName(longName);

        // Assert
        Assert.True(result.Length <= 80, $"Result length {result.Length} should be <= 80");
        Assert.Contains("-", result);
        Assert.True(result.Length == 80, $"Truncated name should be exactly 80 characters, got {result.Length}");
    }

    [Fact]
    public void SanitizeName_WhenNameIsExactly80Characters_WithSqsLimit_ReturnsOriginalName()
    {
        // Arrange
        var formatter = new TruncatedKebabCaseEndpointNameFormatter(maxQueueNameLength: 80);
        var exactLengthName = new string('a', 80); // 80 characters

        // Act
        var result = formatter.SanitizeName(exactLengthName);

        // Assert
        Assert.Equal(exactLengthName, result);
    }

    [Fact]
    public void SanitizeName_WhenNameIsOver255Characters_WithDefaultLimit_TruncatesAndAddsHash()
    {
        // Arrange
        var formatter = new TruncatedKebabCaseEndpointNameFormatter(); // Default 255 limit
        var longName = new string('a', 300); // 300 characters

        // Act
        var result = formatter.SanitizeName(longName);

        // Assert
        Assert.True(result.Length <= 255, $"Result length {result.Length} should be <= 255");
        Assert.Contains("-", result);
        Assert.True(result.Length == 255, $"Truncated name should be exactly 255 characters, got {result.Length}");
    }

    [Fact]
    public void SanitizeName_WhenNameIsUnder255Characters_WithDefaultLimit_ReturnsOriginalName()
    {
        // Arrange
        var formatter = new TruncatedKebabCaseEndpointNameFormatter(); // Default 255 limit
        var shortName = new string('a', 200); // 200 characters

        // Act
        var result = formatter.SanitizeName(shortName);

        // Assert
        Assert.Equal(shortName, result);
    }

    [Fact]
    public void SanitizeName_DifferentLongNames_ProduceDifferentHashes()
    {
        // Arrange
        var formatter = new TruncatedKebabCaseEndpointNameFormatter(maxQueueNameLength: 80);
        var longName1 = "default-module-approval-threshold-modified-fanout-11a97cdbbfc7434195492a3655fb3358-suffix1";
        var longName2 = "default-module-approval-threshold-modified-fanout-11a97cdbbfc7434195492a3655fb3358-suffix2";

        // Act
        var result1 = formatter.SanitizeName(longName1);
        var result2 = formatter.SanitizeName(longName2);

        // Assert
        Assert.NotEqual(result1, result2);
        Assert.True(result1.Length <= 80);
        Assert.True(result2.Length <= 80);
    }
}