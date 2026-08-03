// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using System.Text;
using SnapCd.Contracts;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services.PolicyEvaluation;

/// <summary>
/// Materializes a policy entity into a directory conftest can evaluate: inline content into its
/// own temp dir, remote sources via a shallow fetch of the pinned revision, local sources at the
/// operator-managed path as-is.
/// </summary>
public static class PolicyMaterializer
{
    public static async Task<string> MaterializeAsync(
        ResolvedPolicy policy,
        string scratchDir,
        PolicyEvaluationSettings settings,
        CancellationToken cancellationToken)
    {
        switch (policy.Kind)
        {
            case PolicySourceKind.Inline:
            {
                if (string.IsNullOrEmpty(policy.PolicyContent))
                    throw new PolicyEvaluationException($"Inline policy '{policy.Name}' has no content");
                var dir = Path.Combine(scratchDir, SanitizeName(policy.Name));
                Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(Path.Combine(dir, "policy.rego"), policy.PolicyContent, cancellationToken);
                return dir;
            }
            case PolicySourceKind.Local:
            {
                if (string.IsNullOrEmpty(policy.Path))
                    throw new PolicyEvaluationException($"Local policy '{policy.Name}' has no path");
                if (!Directory.Exists(policy.Path))
                    throw new PolicyEvaluationException($"Local policy '{policy.Name}' path does not exist on this runner: {policy.Path}");
                return policy.Path;
            }
            case PolicySourceKind.Remote:
                return await MaterializeRemoteAsync(policy, scratchDir, settings, cancellationToken);
            default:
                throw new PolicyEvaluationException($"Unknown policy source kind {policy.Kind} for '{policy.Name}'");
        }
    }

    private static async Task<string> MaterializeRemoteAsync(
        ResolvedPolicy policy,
        string scratchDir,
        PolicyEvaluationSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(policy.RepoUrl) || string.IsNullOrEmpty(policy.Revision))
            throw new PolicyEvaluationException($"Remote policy '{policy.Name}' is missing RepoUrl/Revision");

        var checkoutDir = Path.Combine(scratchDir, SanitizeName(policy.Name), "repo");
        Directory.CreateDirectory(checkoutDir);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.MaterializeTimeoutSeconds));

        // Pinned-revision fetch into an empty repo: works uniformly for branches, tags and commit
        // SHAs (git auth comes from the runner environment, same as module sources).
        await RunGitAsync(checkoutDir, ["init", "--quiet"], policy, timeoutCts.Token);
        await RunGitAsync(checkoutDir, ["remote", "add", "origin", policy.RepoUrl], policy, timeoutCts.Token);
        await RunGitAsync(checkoutDir, ["fetch", "--quiet", "--depth", "1", "origin", policy.Revision], policy, timeoutCts.Token);
        await RunGitAsync(checkoutDir, ["checkout", "--quiet", "FETCH_HEAD"], policy, timeoutCts.Token);

        var dir = string.IsNullOrEmpty(policy.Path) ? checkoutDir : Path.Combine(checkoutDir, policy.Path);
        if (!Directory.Exists(dir))
            throw new PolicyEvaluationException($"Remote policy '{policy.Name}' path '{policy.Path}' does not exist at revision {policy.Revision}");

        return dir;
    }

    private static async Task RunGitAsync(string workingDir, string[] args, ResolvedPolicy policy, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo };
        var stderr = new StringBuilder();
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new PolicyEvaluationException($"Timed out materializing remote policy '{policy.Name}' from {policy.RepoUrl}");
        }

        if (process.ExitCode != 0)
            throw new PolicyEvaluationException(
                $"git {args[0]} failed for remote policy '{policy.Name}' ({policy.RepoUrl} @ {policy.Revision}): {stderr}");
    }

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
