using Microsoft.Extensions.Logging;
using Moq;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleSourceRefresher;

namespace SnapCd.Runner.Tests;

public class GitTests : IDisposable
{
    private readonly Mock<ILogger<Git>> _mockLogger;
    private readonly RunnerTaskContext _taskContext;
    private readonly GitModuleSourceResolver _sourceResolver;
    private readonly Git _git;
    private readonly string _testDirectory;

    public GitTests()
    {
        _mockLogger = new Mock<ILogger<Git>>();

        var mockTaskLogger = new Mock<ILogger>();
        var metadata = new JobMetadata
        {
            ModuleName = "test-module",
            NamespaceName = "test-namespace",
            StackName = "test-stack",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = null
        };

        _taskContext = new RunnerTaskContext(
            Guid.NewGuid(),
            "GitTests",
            mockTaskLogger.Object,
            metadata
        );

        _sourceResolver = new GitModuleSourceResolver();

        _git = new Git(
            _mockLogger.Object,
            _taskContext,
            _sourceResolver
        );

        // Create test directory
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "SnapCdGitTests",
            Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task GetWorkingFiles_ShouldReturnEmptySet_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = await _git.GetWorkingFiles(nonExistentPath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWorkingFiles_ShouldReturnEmptySet_WhenNoTerraformFilesExist()
    {
        // Arrange
        var testDir = Path.Combine(_testDirectory, "empty");
        Directory.CreateDirectory(testDir);

        // Act
        var result = await _git.GetWorkingFiles(testDir);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWorkingFiles_ShouldFindTerraformFiles_WhenTheyExist()
    {
        // Arrange
        var testDir = Path.Combine(_testDirectory, "with-terraform-files");
        Directory.CreateDirectory(testDir);

        // Create terraform-related files
        var terraformLockFile = Path.Combine(testDir, ".terraform.lock.hcl");
        var terraformDir = Path.Combine(testDir, ".terraform");
        Directory.CreateDirectory(terraformDir);
        var moduleFile = Path.Combine(terraformDir, "modules.json");

        await File.WriteAllTextAsync(terraformLockFile, "# lock file");
        await File.WriteAllTextAsync(moduleFile, "{}");

        // Act
        var result = await _git.GetWorkingFiles(testDir);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, f => f.EndsWith(".terraform.lock.hcl"));
        Assert.Contains(result, f => f.EndsWith("modules.json"));
    }

    [Fact]
    public async Task GetWorkingFiles_ShouldFindFilesInTerraformDirectory()
    {
        // Arrange
        var testDir = Path.Combine(_testDirectory, "terraform-dir-test");
        Directory.CreateDirectory(testDir);

        var terraformDir = Path.Combine(testDir, ".terraform");
        var providersDir = Path.Combine(terraformDir, "providers");
        Directory.CreateDirectory(providersDir);

        var providerFile = Path.Combine(providersDir, "provider.json");
        await File.WriteAllTextAsync(providerFile, "{}");

        // Act
        var result = await _git.GetWorkingFiles(testDir);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, f => f.Contains("provider.json"));
    }

    [Fact]
    public async Task GetWorkingFiles_ShouldHandleNestedDirectories()
    {
        // Arrange
        var testDir = Path.Combine(_testDirectory, "nested-test");
        var subDir = Path.Combine(testDir, "subdir");
        Directory.CreateDirectory(subDir);

        var terraformDir1 = Path.Combine(testDir, ".terraform");
        var terraformDir2 = Path.Combine(subDir, ".terraform");
        Directory.CreateDirectory(terraformDir1);
        Directory.CreateDirectory(terraformDir2);

        var file1 = Path.Combine(terraformDir1, "file1.json");
        var file2 = Path.Combine(terraformDir2, "file2.json");
        await File.WriteAllTextAsync(file1, "{}");
        await File.WriteAllTextAsync(file2, "{}");

        // Act
        var result = await _git.GetWorkingFiles(testDir);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 2);
        Assert.Contains(result, f => f.Contains("file1.json"));
        Assert.Contains(result, f => f.Contains("file2.json"));
    }

    [Fact]
    public async Task GetWorkingFiles_ShouldReturnEmptySet_WhenExceptionOccurs()
    {
        // Arrange - use a path that will cause an exception
        var invalidPath = new string(Path.GetInvalidPathChars());

        // Act
        var result = await _git.GetWorkingFiles(invalidPath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNotTrackedGitFiles_ShouldReturnEmptySet_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent-git");

        // Act
        var result = await _git.GetNotTrackedGitFiles(nonExistentPath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact(Skip = "Requires Git to be installed and directory to be a Git repository")]
    public async Task GetNotTrackedGitFiles_ShouldFindUntrackedFiles_InGitRepository()
    {
        // This test requires actual Git installation and a Git repository
        // It's skipped by default for CI/CD environments

        // Arrange - would need to initialize a git repo and create untracked files

        // Act

        // Assert
        Assert.True(true, "Requires Git to be installed");
    }

    [Fact]
    public async Task GetWorkingFiles_ShouldFindTerraformLockFile()
    {
        // Arrange
        var testDir = Path.Combine(_testDirectory, "lock-file-test");
        Directory.CreateDirectory(testDir);

        var lockFile = Path.Combine(testDir, ".terraform.lock.hcl");
        await File.WriteAllTextAsync(lockFile, "# Terraform lock file");

        // Act
        var result = await _git.GetWorkingFiles(testDir);

        // Assert
        Assert.Contains(result, f => f.EndsWith(".terraform.lock.hcl"));
    }

    [Fact]
    public async Task GetWorkingFiles_ShouldNotFindNonTerraformFiles()
    {
        // Arrange
        var testDir = Path.Combine(_testDirectory, "non-terraform-test");
        Directory.CreateDirectory(testDir);

        // Create non-terraform files
        var mainTf = Path.Combine(testDir, "main.tf");
        var variablesTf = Path.Combine(testDir, "variables.tf");
        var readme = Path.Combine(testDir, "README.md");

        await File.WriteAllTextAsync(mainTf, "resource \"null_resource\" \"test\" {}");
        await File.WriteAllTextAsync(variablesTf, "variable \"test\" {}");
        await File.WriteAllTextAsync(readme, "# README");

        // Act
        var result = await _git.GetWorkingFiles(testDir);

        // Assert - should NOT find .tf files or README, only .terraform* files
        Assert.DoesNotContain(result, f => f.EndsWith("main.tf"));
        Assert.DoesNotContain(result, f => f.EndsWith("variables.tf"));
        Assert.DoesNotContain(result, f => f.EndsWith("README.md"));
    }
}
