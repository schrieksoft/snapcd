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
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services;
using SnapCd.Runner.Settings;
using Xunit;

namespace SnapCd.Runner.Tests;

public class PulumiEnginePolicyPackTests : IDisposable
{
    private readonly string _testWorkingDirectory;
    private readonly PulumiEngine _engine;

    public PulumiEnginePolicyPackTests()
    {
        _testWorkingDirectory = Path.Combine(Path.GetTempPath(), "snapcd-pulumipolicy-engine-tests", Guid.NewGuid().ToString("N"));

        var metadata = new JobMetadata
        {
            ModuleName = "m",
            NamespaceName = "n",
            StackName = "s",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = ""
        };

        var workingDirectorySettings = Options.Create(new WorkingDirectorySettings
        {
            WorkingDirectory = _testWorkingDirectory,
            TempDirectory = Path.Combine(_testWorkingDirectory, "temp")
        });

        var taskContext = new RunnerTaskContext(Guid.NewGuid(), "Plan", new Mock<ILogger>().Object, new NullJobLogStream(), metadata);

        _engine = new PulumiEngine(
            taskContext,
            new Mock<ILogger>().Object,
            new ModuleDirectoryService(metadata, workingDirectorySettings),
            new List<string>(),
            new Dictionary<string, string>(),
            new List<EngineFlagEntry>(),
            new List<EngineArrayFlagEntry>()
        );

        Directory.CreateDirectory(_engine.GetInitDir());
        Directory.CreateDirectory(_engine.GetSnapCdDir());
        File.WriteAllText(Path.Combine(_engine.GetSnapCdDir(), "snapcd.env"), "# empty");
    }

    public void Dispose()
    {
        try { Directory.Delete(_testWorkingDirectory, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Plan_Appends_PolicyPack_Flags()
    {
        _engine.SetPolicyPacks(["/packs/a", "/packs/b"]);

        // The pulumi binary isn't present in the test environment; the script is written before
        // the process runs, which is all this test needs.
        try { await _engine.Plan(new Dictionary<string, string>(), null, null); } catch { /* expected */ }

        var script = await File.ReadAllTextAsync(Path.Combine(_engine.GetSnapCdDir(), "plan.sh"));
        Assert.Contains("pulumi preview", script);
        Assert.Contains("--policy-pack /packs/a", script);
        Assert.Contains("--policy-pack /packs/b", script);
    }

    [Fact]
    public async Task PlanDestroy_Never_Appends_PolicyPack_Flags()
    {
        // The pulumi CLI rejects --policy-pack on destroy; CrossGuard is apply-side only.
        _engine.SetPolicyPacks(["/packs/a"]);

        try { await _engine.PlanDestroy(new Dictionary<string, string>(), null, null); } catch { /* expected */ }

        var script = await File.ReadAllTextAsync(Path.Combine(_engine.GetSnapCdDir(), "plan_destroy.sh"));
        Assert.Contains("pulumi destroy --preview-only", script);
        Assert.DoesNotContain("--policy-pack", script);
    }

    [Fact]
    public async Task No_Packs_Means_No_Flags()
    {
        try { await _engine.Plan(new Dictionary<string, string>(), null, null); } catch { /* expected */ }

        var script = await File.ReadAllTextAsync(Path.Combine(_engine.GetSnapCdDir(), "plan.sh"));
        Assert.DoesNotContain("--policy-pack", script);
    }
}
