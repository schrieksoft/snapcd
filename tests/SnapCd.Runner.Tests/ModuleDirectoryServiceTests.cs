// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tests;

public class ModuleDirectoryServiceTests
{
    private readonly ModuleDirectoryService _service;
    private readonly string _workingDirectory;

    public ModuleDirectoryServiceTests()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "SnapCdTests", Guid.NewGuid().ToString());

        var metadata = new JobMetadata
        {
            ModuleName = "test-module",
            NamespaceName = "test-namespace",
            StackName = "test-stack",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = null
        };

        var settings = Options.Create(new WorkingDirectorySettings
        {
            WorkingDirectory = _workingDirectory,
            TempDirectory = Path.Combine(_workingDirectory, "temp")
        });

        _service = new ModuleDirectoryService(metadata, settings);
    }

    [Fact]
    public void GetWorkingDir_ShouldReturnConfiguredWorkingDirectory()
    {
        // Act
        var result = _service.GetWorkingDir();

        // Assert
        Assert.Equal(_workingDirectory, result);
    }

    [Fact]
    public void GetTempDir_ShouldReturnConfiguredTempDirectory()
    {
        // Act
        var result = _service.GetTempDir();

        // Assert
        Assert.Equal(Path.Combine(_workingDirectory, "temp"), result);
    }

    [Fact]
    public void GetModuleRootDir_ShouldConstructCorrectPath()
    {
        // Act
        var result = _service.GetModuleRootDir();

        // Assert
        var expected = Path.Combine(_workingDirectory, "test-stack", "test-namespace", "test-module");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetInitDir_ShouldReturnModuleRootDir_WhenNoSubdirectory()
    {
        // Act
        var result = _service.GetInitDir();

        // Assert
        var expected = Path.Combine(_workingDirectory, "test-stack", "test-namespace", "test-module");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetInitDir_ShouldIncludeSubdirectory_WhenSubdirectoryProvided()
    {
        // Arrange
        var metadata = new JobMetadata
        {
            ModuleName = "test-module",
            NamespaceName = "test-namespace",
            StackName = "test-stack",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = "terraform/modules/vpc"
        };

        var settings = Options.Create(new WorkingDirectorySettings
        {
            WorkingDirectory = _workingDirectory,
            TempDirectory = Path.Combine(_workingDirectory, "temp")
        });

        var service = new ModuleDirectoryService(metadata, settings);

        // Act
        var result = service.GetInitDir();

        // Assert
        var expected = Path.Combine(_workingDirectory, "test-stack", "test-namespace", "test-module", "terraform/modules/vpc");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSnapCdDir_ShouldReturnSnapCdDirectoryInInitDir()
    {
        // Act
        var result = _service.GetSnapCdDir();

        // Assert
        var expected = Path.Combine(_workingDirectory, "test-stack", "test-namespace", "test-module", ".snapcd");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSnapCdDir_ShouldIncludeSubdirectory_WhenSubdirectoryProvided()
    {
        // Arrange
        var metadata = new JobMetadata
        {
            ModuleName = "my-module",
            NamespaceName = "my-namespace",
            StackName = "my-stack",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = "modules/networking"
        };

        var settings = Options.Create(new WorkingDirectorySettings
        {
            WorkingDirectory = _workingDirectory,
            TempDirectory = Path.Combine(_workingDirectory, "temp")
        });

        var service = new ModuleDirectoryService(metadata, settings);

        // Act
        var result = service.GetSnapCdDir();

        // Assert
        var expected = Path.Combine(_workingDirectory, "my-stack", "my-namespace", "my-module", "modules/networking", ".snapcd");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Constructor_ShouldHandleEmptySubdirectory()
    {
        // Arrange
        var metadata = new JobMetadata
        {
            ModuleName = "test-module",
            NamespaceName = "test-namespace",
            StackName = "test-stack",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = ""
        };

        var settings = Options.Create(new WorkingDirectorySettings
        {
            WorkingDirectory = _workingDirectory,
            TempDirectory = Path.Combine(_workingDirectory, "temp")
        });

        // Act
        var service = new ModuleDirectoryService(metadata, settings);

        // Assert
        Assert.Equal(service.GetModuleRootDir(), service.GetInitDir());
    }

    [Fact]
    public void Constructor_ShouldStoreMetadata()
    {
        // This test verifies that the Metadata field is accessible (it's protected, so we can't access directly in tests)
        // But we can verify the behavior that depends on it

        // Act
        var moduleRoot = _service.GetModuleRootDir();

        // Assert - if metadata was stored correctly, the path should contain the values from metadata
        Assert.Contains("test-stack", moduleRoot);
        Assert.Contains("test-namespace", moduleRoot);
        Assert.Contains("test-module", moduleRoot);
    }

    [Fact]
    public void MultipleInstances_ShouldHaveIsolatedPaths()
    {
        // Arrange
        var metadata1 = new JobMetadata
        {
            ModuleName = "module1",
            NamespaceName = "namespace1",
            StackName = "stack1",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = null
        };

        var metadata2 = new JobMetadata
        {
            ModuleName = "module2",
            NamespaceName = "namespace2",
            StackName = "stack2",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = null
        };

        var settings = Options.Create(new WorkingDirectorySettings
        {
            WorkingDirectory = _workingDirectory,
            TempDirectory = Path.Combine(_workingDirectory, "temp")
        });

        var service1 = new ModuleDirectoryService(metadata1, settings);
        var service2 = new ModuleDirectoryService(metadata2, settings);

        // Act
        var root1 = service1.GetModuleRootDir();
        var root2 = service2.GetModuleRootDir();

        // Assert
        Assert.NotEqual(root1, root2);
        Assert.Contains("module1", root1);
        Assert.Contains("module2", root2);
    }
}
