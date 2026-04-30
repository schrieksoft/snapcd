using Serilog.Events;
using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tests;

public class LogMessageHelperTests
{
    [Fact]
    public void GetStringProperty_ShouldReturnValue_WhenPropertyExists()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            { "TestProperty", new ScalarValue("test-value") }
        };

        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            properties.Select(kvp => new LogEventProperty(kvp.Key, kvp.Value)));

        // Act
        var result = LogMessageHelper.GetStringProperty(logEvent, "TestProperty");

        // Assert
        Assert.Equal("test-value", result);
    }

    [Fact]
    public void GetStringProperty_ShouldReturnEmptyString_WhenPropertyDoesNotExist()
    {
        // Arrange
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());

        // Act
        var result = LogMessageHelper.GetStringProperty(logEvent, "NonExistent");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetStringProperty_ShouldTrimQuotes()
    {
        // Arrange
        // ScalarValue.ToString() adds quotes around string values, so "quoted-value" becomes "\"quoted-value\""
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            { "QuotedProperty", new ScalarValue("quoted-value") }
        };

        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            properties.Select(kvp => new LogEventProperty(kvp.Key, kvp.Value)));

        // Act
        var result = LogMessageHelper.GetStringProperty(logEvent, "QuotedProperty");

        // Assert
        // Trim('"') removes the quotes that ScalarValue.ToString() added
        Assert.Equal("quoted-value", result);
    }

    [Fact]
    public void GetGuidProperty_ShouldReturnGuid_WhenPropertyIsValidGuid()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            { "GuidProperty", new ScalarValue(expectedGuid.ToString()) }
        };

        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            properties.Select(kvp => new LogEventProperty(kvp.Key, kvp.Value)));

        // Act
        var result = LogMessageHelper.GetGuidProperty(logEvent, "GuidProperty");

        // Assert
        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public void GetGuidProperty_ShouldReturnEmptyGuid_WhenPropertyDoesNotExist()
    {
        // Arrange
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());

        // Act
        var result = LogMessageHelper.GetGuidProperty(logEvent, "NonExistent");

        // Assert
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public void GetGuidProperty_ShouldReturnEmptyGuid_WhenPropertyIsNotValidGuid()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            { "InvalidGuid", new ScalarValue("not-a-guid") }
        };

        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            properties.Select(kvp => new LogEventProperty(kvp.Key, kvp.Value)));

        // Act
        var result = LogMessageHelper.GetGuidProperty(logEvent, "InvalidGuid");

        // Assert
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public void TrimQuotes_ShouldRemoveQuotes_WhenStringHasQuotes()
    {
        // Arrange
        var input = "\"quoted-string\"";

        // Act
        var result = LogMessageHelper.TrimQuotes(input);

        // Assert
        Assert.Equal("quoted-string", result);
    }

    [Fact]
    public void TrimQuotes_ShouldReturnOriginal_WhenStringHasNoQuotes()
    {
        // Arrange
        var input = "unquoted-string";

        // Act
        var result = LogMessageHelper.TrimQuotes(input);

        // Assert
        Assert.Equal("unquoted-string", result);
    }

    [Fact]
    public void TrimQuotes_ShouldReturnOriginal_WhenOnlyStartQuote()
    {
        // Arrange
        var input = "\"only-start";

        // Act
        var result = LogMessageHelper.TrimQuotes(input);

        // Assert
        Assert.Equal("\"only-start", result);
    }

    [Fact]
    public void TrimQuotes_ShouldReturnOriginal_WhenOnlyEndQuote()
    {
        // Arrange
        var input = "only-end\"";

        // Act
        var result = LogMessageHelper.TrimQuotes(input);

        // Assert
        Assert.Equal("only-end\"", result);
    }

    [Fact]
    public void TrimQuotes_ShouldHandleEmptyString()
    {
        // Arrange
        var input = "";

        // Act
        var result = LogMessageHelper.TrimQuotes(input);

        // Assert
        Assert.Equal("", result);
    }
}
