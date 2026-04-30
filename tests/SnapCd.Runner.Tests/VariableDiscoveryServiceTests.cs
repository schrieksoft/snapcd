using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tests;

/// <summary>
/// Tests for the TerraformVariableDiscoveryService class.
/// Verifies discovery and parsing of Terraform variables from .tf and .tf.json files.
/// Tests only cover properties actually stored: Name, Type, Description, Sensitive, Nullable.
/// </summary>
public class TerraformVariableDiscoveryServiceTests
{
    private readonly TerraformVariableDiscoveryService _service;
    private readonly string _testDataBasePath;

    public TerraformVariableDiscoveryServiceTests()
    {
        _service = new TerraformVariableDiscoveryService();
        _testDataBasePath = Path.Combine(AppContext.BaseDirectory, "TestData", "Terraform");
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Throws_When_Directory_Not_Found()
    {
        // Arrange
        var nonExistentPath = "/path/that/does/not/exist/12345";

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _service.DiscoverVariablesAsync(nonExistentPath));
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Returns_Empty_List_For_Empty_Directory()
    {
        // Arrange
        var emptyDirPath = Path.Combine(_testDataBasePath, "Empty");

        // Act
        var variables = await _service.DiscoverVariablesAsync(emptyDirPath);

        // Assert
        Assert.Empty(variables);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Discovers_Variables_From_Tf_File()
    {
        // Arrange
        var simplePath = Path.Combine(_testDataBasePath, "Simple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(simplePath);

        // Assert
        Assert.Single(variables);
        var envVar = variables[0];
        Assert.Equal("environment", envVar.Name);
        Assert.Equal("string", envVar.Type);
        Assert.Equal("The environment name", envVar.Description);
        Assert.True(envVar.Nullable); // Has default value, so nullable should be true
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Discovers_Multiple_Variables_From_Single_File()
    {
        // Arrange
        var multiplePath = Path.Combine(_testDataBasePath, "Multiple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(multiplePath);

        // Assert
        Assert.Equal(8, variables.Count);

        // Verify we have all expected variables
        var varNames = variables.Select(v => v.Name).ToList();
        Assert.Contains("region", varNames);
        Assert.Contains("instance_count", varNames);
        Assert.Contains("enable_monitoring", varNames);
        Assert.Contains("tags", varNames);
        Assert.Contains("availability_zones", varNames);
        Assert.Contains("api_key", varNames);
        Assert.Contains("optional_value", varNames);
        Assert.Contains("required_value", varNames);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Parses_String_Variables()
    {
        // Arrange
        var multiplePath = Path.Combine(_testDataBasePath, "Multiple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(multiplePath);

        // Assert
        var regionVar = variables.First(v => v.Name == "region");
        Assert.Equal("string", regionVar.Type);
        Assert.Equal("AWS region", regionVar.Description);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Parses_Number_Variables()
    {
        // Arrange
        var multiplePath = Path.Combine(_testDataBasePath, "Multiple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(multiplePath);

        // Assert
        var countVar = variables.First(v => v.Name == "instance_count");
        Assert.Equal("number", countVar.Type);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Parses_Boolean_Variables()
    {
        // Arrange
        var multiplePath = Path.Combine(_testDataBasePath, "Multiple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(multiplePath);

        // Assert
        var boolVar = variables.First(v => v.Name == "enable_monitoring");
        Assert.Equal("bool", boolVar.Type);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Parses_Sensitive_Flag()
    {
        // Arrange
        var multiplePath = Path.Combine(_testDataBasePath, "Multiple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(multiplePath);

        // Assert
        var sensitiveVar = variables.First(v => v.Name == "api_key");
        Assert.True(sensitiveVar.Sensitive);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Parses_Nullable_Flag()
    {
        // Arrange
        var multiplePath = Path.Combine(_testDataBasePath, "Multiple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(multiplePath);

        // Assert
        var nullableVar = variables.First(v => v.Name == "optional_value");
        Assert.True(nullableVar.Nullable);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Calculates_Nullable_For_Variable_With_Default()
    {
        // Arrange
        var simplePath = Path.Combine(_testDataBasePath, "Simple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(simplePath);

        // Assert - variable with default should have Nullable = true
        var envVar = variables.First(v => v.Name == "environment");
        Assert.True(envVar.Nullable);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Calculates_Nullable_For_Required_Variable()
    {
        // Arrange
        var multiplePath = Path.Combine(_testDataBasePath, "Multiple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(multiplePath);

        // Assert - variable without default should have Nullable = false
        var requiredVar = variables.First(v => v.Name == "required_value");
        Assert.False(requiredVar.Nullable);
    }

    [Fact]
    public async Task ParseFileAsync_Throws_When_File_Not_Found()
    {
        // Arrange
        var nonExistentFile = "/path/to/nonexistent.tf";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.ParseFileAsync(nonExistentFile));
    }

    [Fact]
    public async Task ParseFileAsync_Parses_Tf_File()
    {
        // Arrange
        var filePath = Path.Combine(_testDataBasePath, "Simple", "simple.tf");

        // Act
        var variables = await _service.ParseFileAsync(filePath);

        // Assert
        Assert.Single(variables);
        Assert.Equal("environment", variables[0].Name);
    }

    [Fact]
    public async Task ParseFileAsync_Throws_For_Unsupported_File_Type()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), "test.txt");
        await File.WriteAllTextAsync(tempFile, "test content");

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                _service.ParseFileAsync(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseFileAsync_Handles_Empty_Tf_File()
    {
        // Arrange
        var filePath = Path.Combine(_testDataBasePath, "Empty", "empty.tf");

        // Act
        var variables = await _service.ParseFileAsync(filePath);

        // Assert
        Assert.Empty(variables);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Discovers_Unquoted_Variable_Names()
    {
        // Arrange
        var unquotedPath = Path.Combine(_testDataBasePath, "Unquoted");

        // Act
        var variables = await _service.DiscoverVariablesAsync(unquotedPath);

        // Assert - should find all 10 variables (8 unquoted + 2 quoted)
        Assert.Equal(10, variables.Count);

        var varNames = variables.Select(v => v.Name).ToList();
        // Unquoted variables
        Assert.Contains("subscription_id", varNames);
        Assert.Contains("tenant_id", varNames);
        Assert.Contains("name", varNames);
        Assert.Contains("location", varNames);
        Assert.Contains("fqdn", varNames);
        Assert.Contains("resource_group_name", varNames);
        Assert.Contains("backend_akv_id", varNames);
        Assert.Contains("cloudflare_zone_id", varNames);
        // Quoted variables
        Assert.Contains("enable_custom_dns", varNames);
        Assert.Contains("proxied", varNames);
    }

    [Fact]
    public async Task DiscoverVariablesAsync_Parses_Unquoted_Variables_With_Empty_Body()
    {
        // Arrange
        var unquotedPath = Path.Combine(_testDataBasePath, "Unquoted");

        // Act
        var variables = await _service.DiscoverVariablesAsync(unquotedPath);

        // Assert - unquoted variable with empty body should have defaults
        var subscriptionIdVar = variables.First(v => v.Name == "subscription_id");
        Assert.Null(subscriptionIdVar.Type);
        Assert.Null(subscriptionIdVar.Description);
        Assert.False(subscriptionIdVar.Sensitive);
        Assert.False(subscriptionIdVar.Nullable); // No default, so not nullable
    }

    [Fact]
    public async Task DiscoverVariablesAsync_FromExtraFile_False_By_Default()
    {
        // Arrange
        var simplePath = Path.Combine(_testDataBasePath, "Simple");

        // Act
        var variables = await _service.DiscoverVariablesAsync(simplePath);

        // Assert - without extra file names, all variables should have FromExtraFile = false
        Assert.All(variables, v => Assert.False(v.FromExtraFile));
    }

    [Fact]
    public async Task DiscoverVariablesAsync_FromExtraFile_True_When_File_In_ExtraFileNames()
    {
        // Arrange
        var simplePath = Path.Combine(_testDataBasePath, "Simple");
        var extraFileNames = new HashSet<string> { "simple.tf" };

        // Act
        var variables = await _service.DiscoverVariablesAsync(simplePath, extraFileNames);

        // Assert - variables from simple.tf should have FromExtraFile = true
        Assert.All(variables, v => Assert.True(v.FromExtraFile));
    }

    [Fact]
    public async Task DiscoverVariablesAsync_FromExtraFile_Mixed_Sources()
    {
        // Arrange
        var unquotedPath = Path.Combine(_testDataBasePath, "Unquoted");
        var extraFileNames = new HashSet<string> { "unquoted.tf" };

        // Act
        var variables = await _service.DiscoverVariablesAsync(unquotedPath, extraFileNames);

        // Assert - all variables from unquoted.tf should have FromExtraFile = true
        Assert.All(variables, v => Assert.True(v.FromExtraFile));
    }

    [Fact]
    public async Task DiscoverVariablesAsync_FromExtraFile_False_When_File_Not_In_ExtraFileNames()
    {
        // Arrange
        var simplePath = Path.Combine(_testDataBasePath, "Simple");
        var extraFileNames = new HashSet<string> { "other.tf" }; // Not the actual filename

        // Act
        var variables = await _service.DiscoverVariablesAsync(simplePath, extraFileNames);

        // Assert - variables from simple.tf should have FromExtraFile = false
        Assert.All(variables, v => Assert.False(v.FromExtraFile));
    }

    #region Output Discovery Tests

    [Fact]
    public async Task DiscoverOutputSourcesAsync_Returns_Empty_For_Empty_Directory()
    {
        // Arrange
        var emptyDirPath = Path.Combine(_testDataBasePath, "Empty");

        // Act
        var outputs = await _service.DiscoverOutputSourcesAsync(emptyDirPath);

        // Assert
        Assert.Empty(outputs);
    }

    [Fact]
    public async Task DiscoverOutputSourcesAsync_Discovers_Quoted_Output_Names()
    {
        // Arrange
        var outputsPath = Path.Combine(_testDataBasePath, "Outputs");

        // Act
        var outputs = await _service.DiscoverOutputSourcesAsync(outputsPath);

        // Assert - should find quoted outputs
        Assert.Contains("vpc_id", outputs.Keys);
        Assert.Contains("subnet_ids", outputs.Keys);
    }

    [Fact]
    public async Task DiscoverOutputSourcesAsync_Discovers_Unquoted_Output_Names()
    {
        // Arrange
        var outputsPath = Path.Combine(_testDataBasePath, "Outputs");

        // Act
        var outputs = await _service.DiscoverOutputSourcesAsync(outputsPath);

        // Assert - should find unquoted outputs
        Assert.Contains("instance_ip", outputs.Keys);
        Assert.Contains("database_endpoint", outputs.Keys);
        Assert.Contains("empty_body", outputs.Keys);
    }

    [Fact]
    public async Task DiscoverOutputSourcesAsync_Discovers_All_Outputs()
    {
        // Arrange
        var outputsPath = Path.Combine(_testDataBasePath, "Outputs");

        // Act
        var outputs = await _service.DiscoverOutputSourcesAsync(outputsPath);

        // Assert - should find all 5 outputs (2 quoted + 3 unquoted)
        Assert.Equal(5, outputs.Count);
    }

    [Fact]
    public async Task DiscoverOutputSourcesAsync_FromExtraFile_False_By_Default()
    {
        // Arrange
        var outputsPath = Path.Combine(_testDataBasePath, "Outputs");

        // Act
        var outputs = await _service.DiscoverOutputSourcesAsync(outputsPath);

        // Assert - without extra file names, all outputs should have FromExtraFile = false
        Assert.All(outputs.Values, isExtraFile => Assert.False(isExtraFile));
    }

    [Fact]
    public async Task DiscoverOutputSourcesAsync_FromExtraFile_True_When_File_In_ExtraFileNames()
    {
        // Arrange
        var outputsPath = Path.Combine(_testDataBasePath, "Outputs");
        var extraFileNames = new HashSet<string> { "outputs.tf" };

        // Act
        var outputs = await _service.DiscoverOutputSourcesAsync(outputsPath, extraFileNames);

        // Assert - all outputs should have FromExtraFile = true
        Assert.All(outputs.Values, isExtraFile => Assert.True(isExtraFile));
    }

    [Fact]
    public async Task DiscoverOutputSourcesAsync_Returns_Empty_When_Directory_Not_Found()
    {
        // Arrange
        var nonExistentPath = "/path/that/does/not/exist/12345";

        // Act
        var outputs = await _service.DiscoverOutputSourcesAsync(nonExistentPath);

        // Assert - returns empty dictionary for graceful handling
        Assert.Empty(outputs);
    }

    #endregion
}