using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tests;

public class VersionServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeVersion()
    {
        // Arrange & Act
        var service = new VersionService();

        // Assert
        Assert.NotNull(service.Version);
        Assert.NotEmpty(service.Version);
    }

    [Fact]
    public void Version_ShouldReturnNonEmptyString()
    {
        // Arrange
        var service = new VersionService();

        // Act
        var version = service.Version;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }

    [Fact]
    public void ShortVersion_ShouldReturnVersionWithoutBuildMetadata()
    {
        // Arrange
        var service = new VersionService();

        // Act
        var shortVersion = service.ShortVersion;

        // Assert
        Assert.NotNull(shortVersion);
        Assert.NotEmpty(shortVersion);
        // Short version should not contain '+' (build metadata separator)
        Assert.DoesNotContain("+", shortVersion);
    }

    [Fact]
    public void ShortVersion_ShouldBeSameAsVersion_WhenNoBuildMetadata()
    {
        // Arrange
        var service = new VersionService();
        var version = service.Version;

        // Act
        var shortVersion = service.ShortVersion;

        // Assert
        // If version doesn't contain '+', short version should be same as full version
        if (!version.Contains('+'))
        {
            Assert.Equal(version, shortVersion);
        }
    }

    [Fact]
    public void ShortVersion_ShouldRemoveBuildMetadata_WhenPresent()
    {
        // Arrange
        var service = new VersionService();
        var version = service.Version;

        // Act
        var shortVersion = service.ShortVersion;

        // Assert
        // If version contains '+', short version should be substring before '+'
        if (version.Contains('+'))
        {
            var expectedShortVersion = version.Substring(0, version.IndexOf('+'));
            Assert.Equal(expectedShortVersion, shortVersion);
        }
    }

    [Fact]
    public void ShortVersion_ShouldBeShorterOrEqualToVersion()
    {
        // Arrange
        var service = new VersionService();

        // Act
        var version = service.Version;
        var shortVersion = service.ShortVersion;

        // Assert
        Assert.True(shortVersion.Length <= version.Length);
    }

    [Fact]
    public void Version_ShouldBeConsistent()
    {
        // Arrange
        var service = new VersionService();

        // Act
        var version1 = service.Version;
        var version2 = service.Version;

        // Assert - version should be consistent across multiple calls
        Assert.Equal(version1, version2);
    }

    [Fact]
    public void ShortVersion_ShouldBeConsistent()
    {
        // Arrange
        var service = new VersionService();

        // Act
        var shortVersion1 = service.ShortVersion;
        var shortVersion2 = service.ShortVersion;

        // Assert - short version should be consistent across multiple calls
        Assert.Equal(shortVersion1, shortVersion2);
    }

    [Fact]
    public void MultipleInstances_ShouldReturnSameVersion()
    {
        // Arrange
        var service1 = new VersionService();
        var service2 = new VersionService();

        // Act
        var version1 = service1.Version;
        var version2 = service2.Version;

        // Assert - different instances should return the same version
        Assert.Equal(version1, version2);
    }
}
