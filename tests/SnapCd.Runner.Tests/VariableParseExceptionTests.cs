using SnapCd.Runner.Exceptions;

namespace SnapCd.Runner.Tests;

/// <summary>
/// Tests for the VariableParseException class (SnapCd.Server.Misc.Exceptions.VariableParseException).
///
/// Tests verify:
/// - Constructor overloads properly set Message, FilePath, and InnerException properties
/// - FilePath property can be null and handles various path formats
/// - InnerException preserves stack trace information
/// - Exception inheritance from System.Exception
///
/// VariableParseException is thrown when parsing Terraform variable files (.tf or .tf.json)
/// fails due to syntax errors, malformed JSON, or other parsing issues.
/// </summary>
public class VariableParseExceptionTests
{
    [Fact]
    public void Constructor_With_Message_Sets_Message()
    {
        // Arrange
        var message = "Parse error occurred";

        // Act
        var exception = new VariableParseException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.FilePath);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_With_Message_And_FilePath_Sets_Both()
    {
        // Arrange
        var message = "Parse error in file";
        var filePath = "/path/to/variables.tf";

        // Act
        var exception = new VariableParseException(message, filePath);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(filePath, exception.FilePath);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_With_Message_And_InnerException_Sets_Both()
    {
        // Arrange
        var message = "Failed to parse variable";
        var innerException = new ArgumentException("Invalid argument");

        // Act
        var exception = new VariableParseException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Null(exception.FilePath);
    }

    [Fact]
    public void Constructor_With_All_Parameters_Sets_All_Properties()
    {
        // Arrange
        var message = "Failed to parse variable";
        var innerException = new InvalidOperationException("Inner error");
        var filePath = "/path/to/bad.tf";

        // Act
        var exception = new VariableParseException(message, innerException, filePath);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Equal(filePath, exception.FilePath);
    }

    [Fact]
    public void FilePath_Can_Be_Null()
    {
        // Arrange & Act
        var exception = new VariableParseException("Error", null);

        // Assert
        Assert.Null(exception.FilePath);
    }

    [Fact]
    public void Exception_Is_Of_Type_Exception()
    {
        // Arrange & Act
        var exception = new VariableParseException("Error");

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("simple.tf")]
    [InlineData("/absolute/path/to/file.tf")]
    [InlineData("../relative/path.tf.json")]
    [InlineData("C:\\Windows\\Path\\file.tf")]
    public void FilePath_Handles_Different_Path_Formats(string path)
    {
        // Arrange & Act
        var exception = new VariableParseException("Error", path);

        // Assert
        Assert.Equal(path, exception.FilePath);
    }

    [Fact]
    public void InnerException_Preserves_Stack_Trace()
    {
        // Arrange
        Exception? capturedInner = null;
        try
        {
            throw new InvalidOperationException("Original error");
        }
        catch (Exception ex)
        {
            capturedInner = ex;
        }

        // Act
        var exception = new VariableParseException("Wrapped error", capturedInner);

        // Assert
        Assert.NotNull(exception.InnerException);
        Assert.NotNull(exception.InnerException.StackTrace);
    }
}