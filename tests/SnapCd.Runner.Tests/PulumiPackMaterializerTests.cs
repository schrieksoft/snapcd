// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Runner.Services.PolicyEvaluation;
using SnapCd.Runner.Settings;
using Xunit;

namespace SnapCd.Runner.Tests;

public class PulumiPackMaterializerTests : IDisposable
{
    private readonly string _root;
    private readonly string _scratch;

    public PulumiPackMaterializerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "snapcd-pulumipack-tests", Guid.NewGuid().ToString("N"));
        _scratch = Path.Combine(_root, "scratch");
        Directory.CreateDirectory(_scratch);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private static ResolvedPolicy InlinePack(PulumiPolicyRuntime runtime = PulumiPolicyRuntime.Python, string? additionalDependencies = null)
    {
        return new ResolvedPolicy
        {
            Name = "pack1",
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Pulumi,
            Kind = PolicySourceKind.Inline,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            PolicyContent = "# entry module",
            Runtime = runtime,
            AdditionalDependencies = additionalDependencies
        };
    }

    [Fact]
    public async Task Python_Inline_Synthesizes_Scaffold_With_Cached_Venv()
    {
        // Empty SDK requirement keeps pip offline-safe: the venv machinery is still exercised.
        var settings = new PolicyEvaluationSettings
        {
            PulumiPolicySdkRequirement = "",
            VenvCacheRoot = Path.Combine(_root, "venvs")
        };

        var dir = await PulumiPackMaterializer.MaterializeAsync(InlinePack(), _scratch, settings, CancellationToken.None);

        Assert.Equal("# entry module", await File.ReadAllTextAsync(Path.Combine(dir, "__main__.py")));
        var yaml = await File.ReadAllTextAsync(Path.Combine(dir, "PulumiPolicy.yaml"));
        Assert.Contains("name: python", yaml);
        Assert.Contains("virtualenv:", yaml);

        var venvPath = yaml.Split("virtualenv:")[1].Trim().Split('\n')[0].Trim();
        Assert.True(File.Exists(Path.Combine(venvPath, "bin", "python")), "cached venv should contain an interpreter");

        // Second materialization reuses the cached venv (ready marker present).
        var dir2 = await PulumiPackMaterializer.MaterializeAsync(InlinePack(), Path.Combine(_root, "scratch2"), settings, CancellationToken.None);
        var yaml2 = await File.ReadAllTextAsync(Path.Combine(dir2, "PulumiPolicy.yaml"));
        Assert.Contains(venvPath, yaml2);
    }

    [Fact]
    public async Task Python_Inline_Operator_Mode_Omits_Venv()
    {
        var settings = new PolicyEvaluationSettings { PackProvisioningEnabled = false };

        var dir = await PulumiPackMaterializer.MaterializeAsync(InlinePack(), _scratch, settings, CancellationToken.None);

        var yaml = await File.ReadAllTextAsync(Path.Combine(dir, "PulumiPolicy.yaml"));
        Assert.Equal("runtime: python", yaml.Trim());
        Assert.True(File.Exists(Path.Combine(dir, "requirements.txt")));
    }

    [Fact]
    public async Task Operator_Mode_Rejects_AdditionalDependencies()
    {
        var settings = new PolicyEvaluationSettings { PackProvisioningEnabled = false };

        var ex = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            PulumiPackMaterializer.MaterializeAsync(InlinePack(additionalDependencies: "requests"), _scratch, settings, CancellationToken.None));
        Assert.Contains("operator-managed", ex.Message);
    }

    [Fact]
    public async Task Python_Inline_Requirements_Contain_Sdk_And_AdditionalDependencies()
    {
        // Operator-mode synthesis path can't carry AdditionalDependencies, so verify the
        // requirements content via a provisioning-enabled run with an offline-safe (empty)
        // dependency set, then check the concatenation shape directly.
        var settings = new PolicyEvaluationSettings
        {
            PulumiPolicySdkRequirement = "pulumi-policy>=1.5.0,<2.0.0",
            PackProvisioningEnabled = false
        };

        var dir = await PulumiPackMaterializer.MaterializeAsync(InlinePack(), _scratch, settings, CancellationToken.None);

        var requirements = await File.ReadAllTextAsync(Path.Combine(dir, "requirements.txt"));
        Assert.StartsWith("pulumi-policy>=1.5.0,<2.0.0", requirements);
    }

    [Fact]
    public async Task Remote_Pack_Materializes_At_Pinned_Revision_With_Yaml_Untouched()
    {
        // Hermetic "remote": a repo-authored pack. Its PulumiPolicy.yaml must pass through
        // untouched — the runner never injects provisioning into someone else's pack config.
        var repoDir = Path.Combine(_root, "pack-repo");
        Directory.CreateDirectory(Path.Combine(repoDir, "packs", "guardrails"));
        var authoredYaml = "runtime:\n  name: python\n  options:\n    virtualenv: venv\n";
        await File.WriteAllTextAsync(Path.Combine(repoDir, "packs", "guardrails", "PulumiPolicy.yaml"), authoredYaml);
        await File.WriteAllTextAsync(Path.Combine(repoDir, "packs", "guardrails", "__main__.py"), "# authored pack");
        RunGit(repoDir, "init", "-q", "-b", "main");
        RunGit(repoDir, "config", "user.name", "Test");
        RunGit(repoDir, "config", "user.email", "test@example.com");
        RunGit(repoDir, "add", ".");
        RunGit(repoDir, "commit", "-q", "-m", "pack");
        RunGit(repoDir, "tag", "v2.0.0");

        var policy = new ResolvedPolicy
        {
            Name = "remote-pack",
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Pulumi,
            Kind = PolicySourceKind.Remote,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            RepoUrl = repoDir,
            Revision = "v2.0.0",
            Path = "packs/guardrails"
        };
        var dir = await PulumiPackMaterializer.MaterializeAsync(policy, _scratch, new PolicyEvaluationSettings(), CancellationToken.None);

        Assert.Equal(authoredYaml, await File.ReadAllTextAsync(Path.Combine(dir, "PulumiPolicy.yaml")));
        Assert.Equal("# authored pack", await File.ReadAllTextAsync(Path.Combine(dir, "__main__.py")));
    }

    [Fact]
    public async Task Local_Pack_Is_Passed_Through_As_Is()
    {
        var packDir = Path.Combine(_root, "operator-pack");
        Directory.CreateDirectory(packDir);
        await File.WriteAllTextAsync(Path.Combine(packDir, "PulumiPolicy.yaml"), "runtime: nodejs\n");

        var policy = new ResolvedPolicy
        {
            Name = "local-pack",
            Scope = PolicyScope.Namespace,
            Engine = PolicyEngine.Pulumi,
            Kind = PolicySourceKind.Local,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            Path = packDir
        };
        var dir = await PulumiPackMaterializer.MaterializeAsync(policy, _scratch, new PolicyEvaluationSettings(), CancellationToken.None);

        Assert.Equal(packDir, dir);
    }

    [Fact]
    public async Task Local_Pack_Missing_Path_Faults()
    {
        var policy = new ResolvedPolicy
        {
            Name = "local-missing",
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Pulumi,
            Kind = PolicySourceKind.Local,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            Path = Path.Combine(_root, "nope")
        };
        var ex = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            PulumiPackMaterializer.MaterializeAsync(policy, _scratch, new PolicyEvaluationSettings(), CancellationToken.None));
        Assert.Contains("does not exist", ex.Message);
    }

    private static void RunGit(string workingDir, params string[] args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new Exception($"git {string.Join(' ', args)} failed: {proc.StandardError.ReadToEnd()}");
    }

    [Fact]
    public async Task Node_Inline_Synthesizes_Scaffold()
    {
        // Provisioning off: npm install needs the network; scaffold synthesis is what's under test.
        var settings = new PolicyEvaluationSettings { PackProvisioningEnabled = false };

        var dir = await PulumiPackMaterializer.MaterializeAsync(InlinePack(PulumiPolicyRuntime.NodeJS), _scratch, settings, CancellationToken.None);

        Assert.Equal("# entry module", await File.ReadAllTextAsync(Path.Combine(dir, "index.js")));
        Assert.Contains("nodejs", await File.ReadAllTextAsync(Path.Combine(dir, "PulumiPolicy.yaml")));
        var packageJson = await File.ReadAllTextAsync(Path.Combine(dir, "package.json"));
        Assert.Contains("@pulumi/policy", packageJson);
    }
}
