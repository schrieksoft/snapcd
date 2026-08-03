// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services.PolicyEvaluation;

public class PolicyEvaluationException : Exception
{
    public PolicyEvaluationException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public class PolicyEntityResult
{
    public required ResolvedPolicy Policy { get; init; }
    public required PolicyOutcome Outcome { get; init; }
    public List<ConftestNamespaceResult> Namespaces { get; init; } = new();
}

/// <summary>
/// Evaluates Terraform/OpenTofu policies with conftest against the JSON export of a plan.
/// Each policy entity is one independent evaluation: inline content in its own temp dir, remote
/// sources at their pinned revision, local sources at their operator-managed path. Severity comes
/// from conftest's own classification; the aggregate is the worst outcome across all entities.
/// </summary>
public class PolicyEvaluationService
{
    private readonly PolicyEvaluationSettings _settings;
    private readonly ILogger<PolicyEvaluationService> _logger;

    public PolicyEvaluationService(IOptions<PolicyEvaluationSettings> settings, ILogger<PolicyEvaluationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PolicyOutcome> EvaluateAsync(
        List<ResolvedPolicy> policies,
        string planJsonPath,
        string scratchDir,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(planJsonPath))
            throw new PolicyEvaluationException($"Plan JSON not found at {planJsonPath}");

        var aggregate = PolicyOutcome.Passed;

        foreach (var policy in policies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await EvaluateEntityAsync(policy, planJsonPath, scratchDir, log, cancellationToken);

            if (result.Outcome == PolicyOutcome.HardDenied)
                aggregate = PolicyOutcome.HardDenied;
            else if (result.Outcome == PolicyOutcome.SoftWarned && aggregate == PolicyOutcome.Passed)
                aggregate = PolicyOutcome.SoftWarned;
        }

        return aggregate;
    }

    public async Task<PolicyEntityResult> EvaluateEntityAsync(
        ResolvedPolicy policy,
        string planJsonPath,
        string scratchDir,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        var policyDir = await PolicyMaterializer.MaterializeAsync(policy, scratchDir, _settings, cancellationToken);

        var (exitCode, stdout, stderr) = await RunConftestAsync(policyDir, planJsonPath, cancellationToken);

        // conftest exits 1 for policy failures too — classify from the JSON, never the exit code.
        List<ConftestNamespaceResult> namespaces;
        try
        {
            namespaces = ConftestResultParser.Parse(stdout);
        }
        catch (ConftestParseException)
        {
            // No parseable JSON means evaluation broke (parse/compile error, bad invocation) rather
            // than a policy decision.
            throw new PolicyEvaluationException(
                $"Policy '{policy.Name}' failed to evaluate (exit {exitCode}): {Truncate(FirstNonEmpty(stderr, stdout), 4000)}");
        }

        if (ConftestResultParser.DefinesNoPolicyRules(namespaces))
            throw new PolicyEvaluationException(
                $"Policy '{policy.Name}' defines no policy rules (no deny/violation/warn rules were evaluated) — check the rule names. Refusing to treat a policy that gates nothing as passed.");

        var failures = namespaces.Sum(n => n.Failures.Count);
        var warnings = namespaces.Sum(n => n.Warnings.Count);
        var outcome = failures > 0 ? PolicyOutcome.HardDenied
            : warnings > 0 ? PolicyOutcome.SoftWarned
            : PolicyOutcome.Passed;

        log($"Policy '{policy.Name}' ({policy.Scope}/{policy.Kind}): {outcome} — {failures} failure(s), {warnings} warning(s)");
        foreach (var ns in namespaces)
        {
            foreach (var v in ns.Failures)
                log($"  DENY [{ns.Namespace}] {v.Message}{FormatAttribution(v)}");
            foreach (var v in ns.Warnings)
                log($"  WARN [{ns.Namespace}] {v.Message}{FormatAttribution(v)}");
        }

        return new PolicyEntityResult { Policy = policy, Outcome = outcome, Namespaces = namespaces };
    }

    private static string FormatAttribution(ConftestViolation v)
    {
        var parts = new List<string>();
        if (v.Query != null) parts.Add(v.Query);
        if (v.DetailsJson != null) parts.Add($"details: {v.DetailsJson}");
        return parts.Count > 0 ? $" ({string.Join("; ", parts)})" : "";
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunConftestAsync(
        string policyDir,
        string planJsonPath,
        CancellationToken cancellationToken)
    {
        var binary = string.IsNullOrEmpty(_settings.ConftestBinaryPath) ? "conftest" : _settings.ConftestBinaryPath;

        var startInfo = new ProcessStartInfo
        {
            FileName = binary,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("test");
        startInfo.ArgumentList.Add(planJsonPath);
        startInfo.ArgumentList.Add("--policy");
        startInfo.ArgumentList.Add(policyDir);
        startInfo.ArgumentList.Add("--all-namespaces");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add("json");

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            throw new PolicyEvaluationException(
                $"Failed to start conftest ('{binary}'): {e.Message}. The conftest binary is operator-provided and must be on the runner's PATH.", e);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.EvaluationTimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new PolicyEvaluationException(
                $"conftest evaluation timed out after {_settings.EvaluationTimeoutSeconds}s (OPA has no built-in evaluation timeout; check the policy for unbounded evaluation)");
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw;
        }

        return (process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static string FirstNonEmpty(string a, string b)
    {
        return string.IsNullOrWhiteSpace(a) ? b : a;
    }

    private static string Truncate(string s, int max)
    {
        return s.Length <= max ? s : s[..max];
    }
}
