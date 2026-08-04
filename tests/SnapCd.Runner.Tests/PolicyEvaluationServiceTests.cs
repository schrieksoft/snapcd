// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Runner.Services.PolicyEvaluation;
using SnapCd.Runner.Settings;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// Real-binary tests: require conftest on PATH (operator-provided in dev and CI, like git).
/// A hermetic local git repo backs the remote-source tests.
/// </summary>
public class PolicyEvaluationServiceTests : IDisposable
{
    private readonly string _root;
    private readonly string _planJsonPath;
    private readonly string _scratchDir;
    private readonly PolicyEvaluationService _service;

    private const string PlanJson = """
    {
        "format_version": "1.2",
        "resource_changes": [
            {
                "address": "aws_s3_bucket_public_access_block.logs",
                "type": "aws_s3_bucket_public_access_block",
                "change": { "actions": ["create"], "before": null, "after": { "block_public_acls": false } }
            },
            {
                "address": "aws_db_instance.prod",
                "type": "aws_db_instance",
                "change": { "actions": ["delete"], "before": { "engine": "postgres" }, "after": null }
            }
        ]
    }
    """;

    private const string DenyPolicy = """
    package snapcd

    import rego.v1

    deny contains msg if {
        some r in input.resource_changes
        r.type == "aws_s3_bucket_public_access_block"
        r.change.after != null
        not r.change.after.block_public_acls
        msg := sprintf("%s allows public ACLs", [r.address])
    }
    """;

    private const string WarnOnlyPolicy = """
    package snapcd

    import rego.v1

    warn contains msg if {
        some r in input.resource_changes
        "delete" in r.change.actions
        msg := sprintf("%s will be destroyed", [r.address])
    }
    """;

    private const string PassingPolicy = """
    package snapcd

    import rego.v1

    deny contains msg if {
        some r in input.resource_changes
        r.type == "never_matches_anything"
        msg := "unreachable"
    }
    """;

    private const string TypodRuleNamePolicy = """
    package snapcd

    import rego.v1

    denny contains msg if {
        some r in input.resource_changes
        msg := sprintf("%s bad", [r.address])
    }
    """;

    private const string ViolationPolicy = """
    package snapcd

    import rego.v1

    violation contains v if {
        some r in input.resource_changes
        r.type == "aws_s3_bucket_public_access_block"
        r.change.after != null
        not r.change.after.block_public_acls
        v := {"msg": sprintf("%s allows public ACLs", [r.address]), "details": {"severity": "high", "cve_class": "exposure"}}
    }
    """;

    private const string SyntaxErrorPolicy = "package snapcd\nthis is not valid rego {{{";

    public PolicyEvaluationServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "snapcd-policyeval-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _planJsonPath = Path.Combine(_root, "plan.json");
        File.WriteAllText(_planJsonPath, PlanJson);
        _scratchDir = Path.Combine(_root, "scratch");
        Directory.CreateDirectory(_scratchDir);

        _service = CreateService(new PolicyEvaluationSettings());
    }

    private static PolicyEvaluationService CreateService(PolicyEvaluationSettings settings)
    {
        return new PolicyEvaluationService(Options.Create(settings), NullLogger<PolicyEvaluationService>.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private static ResolvedPolicy Inline(string content, string name = "p1")
    {
        return new ResolvedPolicy
        {
            Name = name,
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Terraform,
            Kind = PolicySourceKind.Inline,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            PolicyContent = content
        };
    }

    [Fact]
    public async Task Deny_Rule_Firing_Is_HardDenied()
    {
        var outcome = await _service.EvaluateAsync([Inline(DenyPolicy)], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None);
        Assert.Equal(PolicyOutcome.HardDenied, outcome);
    }

    [Fact]
    public async Task Violation_Rule_Is_HardDenied_With_Structured_Details_In_Logs()
    {
        var logs = new List<string>();
        var outcome = await _service.EvaluateAsync([Inline(ViolationPolicy)], _planJsonPath, _scratchDir, logs.Add, CancellationToken.None);

        Assert.Equal(PolicyOutcome.HardDenied, outcome);
        var line = Assert.Single(logs.Where(l => l.Contains("allows public ACLs")));
        Assert.Contains("details:", line);
        Assert.Contains("severity", line);
        Assert.Contains("data.snapcd.violation", line);
    }

    [Fact]
    public async Task Warn_Rule_Firing_Is_SoftWarned()
    {
        var outcome = await _service.EvaluateAsync([Inline(WarnOnlyPolicy)], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None);
        Assert.Equal(PolicyOutcome.SoftWarned, outcome);
    }

    [Fact]
    public async Task Clean_Rules_Pass()
    {
        var outcome = await _service.EvaluateAsync([Inline(PassingPolicy)], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None);
        Assert.Equal(PolicyOutcome.Passed, outcome);
    }

    [Fact]
    public async Task Aggregate_Is_Worst_Across_Entities()
    {
        var outcome = await _service.EvaluateAsync(
            [Inline(PassingPolicy, "a"), Inline(WarnOnlyPolicy, "b"), Inline(DenyPolicy, "c")],
            _planJsonPath, _scratchDir, _ => { }, CancellationToken.None);
        Assert.Equal(PolicyOutcome.HardDenied, outcome);
    }

    [Fact]
    public async Task Syntax_Error_Faults_With_Parse_Error()
    {
        var ex = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            _service.EvaluateAsync([Inline(SyntaxErrorPolicy)], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None));
        Assert.Contains("failed to evaluate", ex.Message);
    }

    [Fact]
    public async Task Typod_Rule_Names_Fault_Via_Zero_Rule_Detection()
    {
        // conftest itself exits 0 and reports success for this policy — it silently gates nothing.
        var ex = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            _service.EvaluateAsync([Inline(TypodRuleNamePolicy)], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None));
        Assert.Contains("defines no policy rules", ex.Message);
    }

    [Fact]
    public async Task Missing_Binary_Faults_With_Operator_Hint()
    {
        var service = CreateService(new PolicyEvaluationSettings { ConftestBinaryPath = "/nonexistent/conftest" });
        var ex = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            service.EvaluateAsync([Inline(DenyPolicy)], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None));
        Assert.Contains("operator-provided", ex.Message);
    }

    [Fact]
    public async Task Local_Source_Missing_Path_Faults()
    {
        var policy = new ResolvedPolicy
        {
            Name = "local-missing",
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Terraform,
            Kind = PolicySourceKind.Local,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            Path = Path.Combine(_root, "does-not-exist")
        };
        var ex = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            _service.EvaluateAsync([policy], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None));
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public async Task Local_Source_Bundle_Evaluates()
    {
        var bundleDir = Path.Combine(_root, "local-bundle");
        Directory.CreateDirectory(bundleDir);
        await File.WriteAllTextAsync(Path.Combine(bundleDir, "deny.rego"), DenyPolicy);

        var policy = new ResolvedPolicy
        {
            Name = "local-bundle",
            Scope = PolicyScope.Namespace,
            Engine = PolicyEngine.Terraform,
            Kind = PolicySourceKind.Local,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            Path = bundleDir
        };
        var outcome = await _service.EvaluateAsync([policy], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None);
        Assert.Equal(PolicyOutcome.HardDenied, outcome);
    }

    [Fact]
    public async Task Remote_Source_Pinned_Revision_Evaluates_As_Bundle()
    {
        // Hermetic "remote": a local git repo with a lib + policy tree, fetched by tag.
        var repoDir = Path.Combine(_root, "policy-repo");
        Directory.CreateDirectory(Path.Combine(repoDir, "policies"));
        await File.WriteAllTextAsync(Path.Combine(repoDir, "policies", "warn.rego"), WarnOnlyPolicy);
        RunGit(repoDir, "init", "-q", "-b", "main");
        RunGit(repoDir, "config", "user.name", "Test");
        RunGit(repoDir, "config", "user.email", "test@example.com");
        RunGit(repoDir, "add", ".");
        RunGit(repoDir, "commit", "-q", "-m", "policies");
        RunGit(repoDir, "tag", "v1.0.0");

        var policy = new ResolvedPolicy
        {
            Name = "remote-bundle",
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Terraform,
            Kind = PolicySourceKind.Remote,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            RepoUrl = repoDir,
            Revision = "v1.0.0",
            Path = "policies"
        };
        var outcome = await _service.EvaluateAsync([policy], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None);
        Assert.Equal(PolicyOutcome.SoftWarned, outcome);
    }

    [Fact]
    public async Task Remote_Source_One_Broken_File_Faults_Whole_Bundle()
    {
        var bundleDir = Path.Combine(_root, "broken-bundle");
        Directory.CreateDirectory(bundleDir);
        await File.WriteAllTextAsync(Path.Combine(bundleDir, "good.rego"), DenyPolicy);
        await File.WriteAllTextAsync(Path.Combine(bundleDir, "broken.rego"), SyntaxErrorPolicy);

        var policy = new ResolvedPolicy
        {
            Name = "broken-bundle",
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Terraform,
            Kind = PolicySourceKind.Local,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            Path = bundleDir
        };
        var ex = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            _service.EvaluateAsync([policy], _planJsonPath, _scratchDir, _ => { }, CancellationToken.None));
        Assert.Contains("failed to evaluate", ex.Message);
    }

    private static void RunGit(string workingDir, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0)
            throw new Exception($"git {string.Join(' ', args)} failed: {p.StandardError.ReadToEnd()}");
    }
}
