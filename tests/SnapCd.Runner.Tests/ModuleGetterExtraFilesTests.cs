// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleGetter;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tests;

public class ModuleGetterExtraFilesTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _moduleRootDir;
    private readonly string _snapCdDir;
    private readonly string _backupDir;
    private readonly string _manifestPath;
    private readonly TestableModuleGetter _moduleGetter;

    public ModuleGetterExtraFilesTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SnapCdTests", Guid.NewGuid().ToString());
        _moduleRootDir = Path.Combine(_testDir, "module");
        _snapCdDir = Path.Combine(_moduleRootDir, ".snapcd");
        _backupDir = Path.Combine(_snapCdDir, "original-files");
        _manifestPath = Path.Combine(_snapCdDir, "extra-files.json");

        Directory.CreateDirectory(_moduleRootDir);
        Directory.CreateDirectory(_snapCdDir);

        var metadata = new JobMetadata
        {
            ModuleName = "test-module",
            NamespaceName = "test-namespace",
            StackName = "test-stack",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = null
        };

        var mockLogger = new Mock<ILogger<ModuleGetter>>();
        var realLogger = new Mock<ILogger>().Object;
        var context = new RunnerTaskContext(Guid.NewGuid(), "Test", realLogger, metadata);

        var mockDirectoryService = new Mock<ModuleDirectoryService>(
            metadata,
            Options.Create(new WorkingDirectorySettings
            {
                WorkingDirectory = _testDir,
                TempDirectory = Path.Combine(_testDir, "temp")
            }));

        mockDirectoryService.Setup(x => x.GetModuleRootDir()).Returns(_moduleRootDir);
        mockDirectoryService.Setup(x => x.GetInitDir()).Returns(_moduleRootDir);
        mockDirectoryService.Setup(x => x.GetSnapCdDir()).Returns(_snapCdDir);
        mockDirectoryService.Setup(x => x.GetWorkingDir()).Returns(_testDir);
        mockDirectoryService.Setup(x => x.GetTempDir()).Returns(Path.Combine(_testDir, "temp"));

        _moduleGetter = new TestableModuleGetter(
            mockDirectoryService.Object,
            context,
            mockLogger.Object
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public async Task AddExtraFiles_CreatesNewFile_TrackedInManifest()
    {
        // Arrange
        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "new-file.tf", Contents = "# new content", Overwrite = false }
        };

        // Act
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        // Assert
        var filePath = Path.Combine(_moduleRootDir, "new-file.tf");
        Assert.True(File.Exists(filePath));
        Assert.Equal("# new content", await File.ReadAllTextAsync(filePath));

        var manifest = await ReadManifest();
        Assert.Contains("new-file.tf", manifest.Created);
        Assert.Empty(manifest.Overwritten);
    }

    [Fact]
    public async Task AddExtraFiles_OverwriteTrue_BacksUpOriginalAndTracksAsOverwritten()
    {
        // Arrange
        var originalContent = "# original content";
        var newContent = "# overwritten content";
        var filePath = Path.Combine(_moduleRootDir, "existing.tf");
        await File.WriteAllTextAsync(filePath, originalContent);

        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "existing.tf", Contents = newContent, Overwrite = true }
        };

        // Act
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        // Assert
        Assert.Equal(newContent, await File.ReadAllTextAsync(filePath));

        var backupPath = Path.Combine(_backupDir, "existing.tf");
        Assert.True(File.Exists(backupPath));
        Assert.Equal(originalContent, await File.ReadAllTextAsync(backupPath));

        var manifest = await ReadManifest();
        Assert.Empty(manifest.Created);
        Assert.Contains("existing.tf", manifest.Overwritten);
    }

    [Fact]
    public async Task AddExtraFiles_OverwriteFalse_SkipsExistingFileAndDoesNotTrack()
    {
        // Arrange
        var originalContent = "# original content";
        var filePath = Path.Combine(_moduleRootDir, "existing.tf");
        await File.WriteAllTextAsync(filePath, originalContent);

        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "existing.tf", Contents = "# should not be written", Overwrite = false }
        };

        // Act
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        // Assert
        Assert.Equal(originalContent, await File.ReadAllTextAsync(filePath));
        Assert.False(Directory.Exists(_backupDir));

        var manifest = await ReadManifest();
        Assert.Empty(manifest.Created);
        Assert.Empty(manifest.Overwritten);
    }

    [Fact]
    public async Task AddExtraFiles_DeletesCreatedFileWhenRemoved()
    {
        // Arrange - first run creates a file
        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "temp-file.tf", Contents = "# temp content", Overwrite = false }
        };
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        var filePath = Path.Combine(_moduleRootDir, "temp-file.tf");
        Assert.True(File.Exists(filePath));

        // Act - second run with empty extra files list
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), new List<ExtraFileDto>());

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task AddExtraFiles_RestoresOriginalWhenOverwrittenFileRemoved()
    {
        // Arrange - first run overwrites a file
        var originalContent = "# original content";
        var filePath = Path.Combine(_moduleRootDir, "config.tf");
        await File.WriteAllTextAsync(filePath, originalContent);

        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "config.tf", Contents = "# overwritten", Overwrite = true }
        };
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        Assert.Equal("# overwritten", await File.ReadAllTextAsync(filePath));

        // Act - second run with empty extra files list
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), new List<ExtraFileDto>());

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.Equal(originalContent, await File.ReadAllTextAsync(filePath));

        var backupPath = Path.Combine(_backupDir, "config.tf");
        Assert.False(File.Exists(backupPath)); // Backup should be deleted after restore
    }

    [Fact]
    public async Task AddExtraFiles_WorkingFiles_AlwaysWrittenNotTracked()
    {
        // Arrange
        var workingFilePath = Path.Combine(_moduleRootDir, ".terraform.lock.hcl");
        var originalContent = "# original terraform lock";
        await File.WriteAllTextAsync(workingFilePath, originalContent);

        var workingFilePaths = new HashSet<string> { workingFilePath };
        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = ".terraform.lock.hcl", Contents = "# new lock content", Overwrite = false }
        };

        // Act
        await _moduleGetter.TestAddExtraFiles(workingFilePaths, extraFiles);

        // Assert - file should be overwritten even though Overwrite=false
        Assert.Equal("# new lock content", await File.ReadAllTextAsync(workingFilePath));

        // But not tracked in manifest (no backup, no cleanup needed)
        var manifest = await ReadManifest();
        Assert.Empty(manifest.Created);
        Assert.Empty(manifest.Overwritten);
    }

    [Fact]
    public async Task AddExtraFiles_MissingBackup_DeletesOverwrittenFile()
    {
        // Arrange - simulate a manifest with overwritten file but missing backup
        var manifest = new { Created = new List<string>(), Overwritten = new List<string> { "orphan.tf" } };
        await File.WriteAllTextAsync(_manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest));

        var filePath = Path.Combine(_moduleRootDir, "orphan.tf");
        await File.WriteAllTextAsync(filePath, "# some content");

        // Act - run with empty extra files (should try to restore)
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), new List<ExtraFileDto>());

        // Assert - file should be deleted since backup is missing
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task AddExtraFiles_NullExtraFiles_CleansUpPreviousFiles()
    {
        // Arrange - first run creates a file
        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "created.tf", Contents = "# content", Overwrite = false }
        };
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        var filePath = Path.Combine(_moduleRootDir, "created.tf");
        Assert.True(File.Exists(filePath));

        // Act - run with null extra files
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), null);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task AddExtraFiles_MultipleFiles_TrackedCorrectly()
    {
        // Arrange
        var existingPath = Path.Combine(_moduleRootDir, "existing.tf");
        await File.WriteAllTextAsync(existingPath, "# original");

        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "new1.tf", Contents = "# new 1", Overwrite = false },
            new() { FileName = "new2.tf", Contents = "# new 2", Overwrite = false },
            new() { FileName = "existing.tf", Contents = "# overwritten", Overwrite = true }
        };

        // Act
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        // Assert
        var manifest = await ReadManifest();
        Assert.Equal(2, manifest.Created.Count);
        Assert.Contains("new1.tf", manifest.Created);
        Assert.Contains("new2.tf", manifest.Created);
        Assert.Single(manifest.Overwritten);
        Assert.Contains("existing.tf", manifest.Overwritten);
    }

    [Fact]
    public async Task AddExtraFiles_BackupNotOverwrittenOnSubsequentRuns()
    {
        // Arrange - first run overwrites a file
        var originalContent = "# original";
        var filePath = Path.Combine(_moduleRootDir, "config.tf");
        await File.WriteAllTextAsync(filePath, originalContent);

        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "config.tf", Contents = "# version 1", Overwrite = true }
        };
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        // Act - second run with different content
        extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "config.tf", Contents = "# version 2", Overwrite = true }
        };
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        // Assert - backup should still contain original, not version 1
        var backupPath = Path.Combine(_backupDir, "config.tf");
        Assert.Equal(originalContent, await File.ReadAllTextAsync(backupPath));
        Assert.Equal("# version 2", await File.ReadAllTextAsync(filePath));
    }

    private async Task<ManifestData> ReadManifest()
    {
        if (!File.Exists(_manifestPath))
            return new ManifestData();

        var json = await File.ReadAllTextAsync(_manifestPath);
        return System.Text.Json.JsonSerializer.Deserialize<ManifestData>(json) ?? new ManifestData();
    }

    private class ManifestData
    {
        public List<string> Created { get; set; } = new();
        public List<string> Overwritten { get; set; } = new();
    }

    private class TestableModuleGetter : ModuleGetter
    {
        public TestableModuleGetter(
            ModuleDirectoryService moduleDirectoryService,
            RunnerTaskContext context,
            ILogger<ModuleGetter> logger)
            : base(
                SourceRevisionType.Default,
                "http://test.git",
                "main",
                null,
                moduleDirectoryService,
                context,
                logger,
                "tofu")
        {
        }

        public Task TestAddExtraFiles(HashSet<string> workingFilePaths, List<ExtraFileDto>? extraFiles)
        {
            return AddExtraFiles(workingFilePaths, extraFiles);
        }

        protected override Task<HashSet<string>> GetWorkingFiles() => Task.FromResult(new HashSet<string>());
        protected override Task<string> StashWorkingFiles(HashSet<string> workingFilePaths) => Task.FromResult("");
        protected override Task UnstashWorkingFiles(HashSet<string> workingFilePaths, string tempDir) => Task.CompletedTask;
        protected override Task<string> GetLocalDefinitiveRevision() => Task.FromResult("");
        public override Task<string> GetRemoteDefinitiveRevision() => Task.FromResult("");
        public override Task<string> GetRemoteResolvedRevision() => Task.FromResult("");
        protected override Task DownloadModule(string resolvedRevision) => Task.CompletedTask;
        protected override Task<bool> ConfirmModuleDownloaded() => Task.FromResult(true);
    }
}
