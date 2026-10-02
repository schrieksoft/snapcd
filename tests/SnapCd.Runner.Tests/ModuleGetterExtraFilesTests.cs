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
using SnapCd.Runner.Logging;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleGetter;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tests;

public class ModuleGetterExtraFilesTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _moduleRootDir;
    private readonly string _snapCdDir;
    private readonly TestableModuleGetter _moduleGetter;

    public ModuleGetterExtraFilesTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SnapCdTests", Guid.NewGuid().ToString());
        _moduleRootDir = Path.Combine(_testDir, "module");
        _snapCdDir = Path.Combine(_moduleRootDir, ".snapcd");

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
        var context = new RunnerTaskContext(Guid.NewGuid(), "Test", realLogger, new NullJobLogStream(), metadata);

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
    public async Task AddExtraFiles_WritesAFileThatIsNotThere()
    {
        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "backend.tf", Contents = "terraform {}", Overwrite = false }
        };

        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        Assert.Equal("terraform {}", await File.ReadAllTextAsync(Path.Combine(_moduleRootDir, "backend.tf")));
    }

    // The checkout matches the source when extra files are applied, so a file already at the path
    // is one the source ships.
    [Fact]
    public async Task AddExtraFiles_OverwriteFalse_LeavesTheSourcesOwnFile()
    {
        var filePath = Path.Combine(_moduleRootDir, "existing.tf");
        await File.WriteAllTextAsync(filePath, "# from the source");

        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "existing.tf", Contents = "# should not be written", Overwrite = false }
        };

        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        Assert.Equal("# from the source", await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task AddExtraFiles_OverwriteTrue_ReplacesTheSourcesOwnFile()
    {
        var filePath = Path.Combine(_moduleRootDir, "existing.tf");
        await File.WriteAllTextAsync(filePath, "# from the source");

        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "existing.tf", Contents = "# from Snap CD", Overwrite = true }
        };

        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        Assert.Equal("# from Snap CD", await File.ReadAllTextAsync(filePath));
    }

    // Working files are the engine's own state, carried across a re-download rather than shipped
    // by the source, so Overwrite does not apply to them.
    [Fact]
    public async Task AddExtraFiles_WorkingFile_IsWrittenEvenWithoutOverwrite()
    {
        var filePath = Path.Combine(_moduleRootDir, "terraform.tfstate");
        await File.WriteAllTextAsync(filePath, "{}");

        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "terraform.tfstate", Contents = "{\"version\":4}", Overwrite = false }
        };

        await _moduleGetter.TestAddExtraFiles(new HashSet<string> { filePath }, extraFiles);

        Assert.Equal("{\"version\":4}", await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task AddExtraFiles_MultipleFiles_AllWritten()
    {
        var extraFiles = new List<ExtraFileDto>
        {
            new() { FileName = "a.tf", Contents = "a", Overwrite = false },
            new() { FileName = "b.tf", Contents = "b", Overwrite = false }
        };

        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), extraFiles);

        Assert.Equal("a", await File.ReadAllTextAsync(Path.Combine(_moduleRootDir, "a.tf")));
        Assert.Equal("b", await File.ReadAllTextAsync(Path.Combine(_moduleRootDir, "b.tf")));
    }

    [Fact]
    public async Task AddExtraFiles_NoExtraFiles_WritesNothing()
    {
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), null);
        await _moduleGetter.TestAddExtraFiles(new HashSet<string>(), new List<ExtraFileDto>());

        Assert.Empty(Directory.GetFiles(_moduleRootDir));
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
        protected override Task ResetToSource(bool includeEngineArtifacts) => Task.CompletedTask;
    }
}
