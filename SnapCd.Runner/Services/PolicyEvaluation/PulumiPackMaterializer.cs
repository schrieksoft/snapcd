// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SnapCd.Contracts;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services.PolicyEvaluation;

/// <summary>
/// Materializes CrossGuard policy packs for `--policy-pack`. Inline packs are synthesized (the
/// entity carries only the entry module; the scaffold and dependency environment are runner-owned).
/// Remote packs are fetched at their pinned revision and are expected to be self-contained (deps
/// vendored, or the SDK preinstalled by the operator). Local packs are operator-managed as-is.
/// </summary>
public static class PulumiPackMaterializer
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> VenvLocks = new();

    public static async Task<string> MaterializeAsync(
        ResolvedPolicy policy,
        string scratchDir,
        PolicyEvaluationSettings settings,
        CancellationToken cancellationToken)
    {
        switch (policy.Kind)
        {
            case PolicySourceKind.Inline:
                return await MaterializeInlineAsync(policy, scratchDir, settings, cancellationToken);
            case PolicySourceKind.Remote:
            case PolicySourceKind.Local:
                return await PolicyMaterializer.MaterializeAsync(policy, scratchDir, settings, cancellationToken);
            default:
                throw new PolicyEvaluationException($"Unknown policy source kind {policy.Kind} for '{policy.Name}'");
        }
    }

    private static async Task<string> MaterializeInlineAsync(
        ResolvedPolicy policy,
        string scratchDir,
        PolicyEvaluationSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(policy.PolicyContent))
            throw new PolicyEvaluationException($"Inline policy pack '{policy.Name}' has no content");

        if (!settings.PackProvisioningEnabled && !string.IsNullOrWhiteSpace(policy.AdditionalDependencies))
            throw new PolicyEvaluationException(
                $"Inline policy pack '{policy.Name}' declares AdditionalDependencies, but this runner is configured for operator-managed policy environments (PackProvisioningEnabled=false).");

        var runtime = policy.Runtime ?? PulumiPolicyRuntime.Python;
        var dir = Path.Combine(scratchDir, SanitizeName(policy.Name));
        Directory.CreateDirectory(dir);

        return runtime switch
        {
            PulumiPolicyRuntime.Python => await SynthesizePythonPackAsync(policy, dir, settings, cancellationToken),
            PulumiPolicyRuntime.NodeJS => await SynthesizeNodePackAsync(policy, dir, settings, cancellationToken),
            _ => throw new PolicyEvaluationException($"Unknown runtime {runtime} for inline policy pack '{policy.Name}'")
        };
    }

    private static async Task<string> SynthesizePythonPackAsync(
        ResolvedPolicy policy,
        string dir,
        PolicyEvaluationSettings settings,
        CancellationToken cancellationToken)
    {
        var requirements = settings.PulumiPolicySdkRequirement + "\n";
        if (!string.IsNullOrWhiteSpace(policy.AdditionalDependencies))
            requirements += policy.AdditionalDependencies.Trim() + "\n";

        await File.WriteAllTextAsync(Path.Combine(dir, "__main__.py"), policy.PolicyContent!, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(dir, "requirements.txt"), requirements, cancellationToken);

        if (!settings.PackProvisioningEnabled)
        {
            // Operator mode: run on the ambient interpreter, SDK preinstalled by the operator.
            await File.WriteAllTextAsync(Path.Combine(dir, "PulumiPolicy.yaml"), "runtime: python\n", cancellationToken);
            return dir;
        }

        var venvPath = await EnsureCachedVenvAsync(requirements, settings, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(dir, "PulumiPolicy.yaml"), $"""
            runtime:
              name: python
              options:
                toolchain: pip
                virtualenv: {venvPath}

            """, cancellationToken);
        return dir;
    }

    private static async Task<string> SynthesizeNodePackAsync(
        ResolvedPolicy policy,
        string dir,
        PolicyEvaluationSettings settings,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(Path.Combine(dir, "index.js"), policy.PolicyContent!, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(dir, "PulumiPolicy.yaml"), "runtime: nodejs\n", cancellationToken);

        var dependencies = new Dictionary<string, string> { ["@pulumi/policy"] = settings.NodePolicySdkRequirement };
        if (!string.IsNullOrWhiteSpace(policy.AdditionalDependencies))
        {
            foreach (var line in policy.AdditionalDependencies.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var at = line.LastIndexOf('@');
                if (at > 0)
                    dependencies[line[..at]] = line[(at + 1)..];
                else
                    dependencies[line] = "*";
            }
        }

        var packageJson = JsonSerializer.Serialize(new
        {
            name = "snapcd-inline-policy",
            main = "index.js",
            dependencies
        }, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(dir, "package.json"), packageJson, cancellationToken);

        if (settings.PackProvisioningEnabled)
            await RunToolAsync(settings.NpmPath, ["install", "--no-audit", "--no-fund", "--loglevel", "error"], dir, settings, cancellationToken);

        return dir;
    }

    private static async Task<string> EnsureCachedVenvAsync(
        string requirements,
        PolicyEvaluationSettings settings,
        CancellationToken cancellationToken)
    {
        var cacheRoot = settings.VenvCacheRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".snapcd", "policy-venvs");
        Directory.CreateDirectory(cacheRoot);

        // Key on the dependency set and the interpreter identity — a venv is only reusable for
        // the exact requirements and Python that built it.
        var pythonVersion = await CaptureToolOutputAsync(settings.PythonPath, ["--version"], cacheRoot, settings, cancellationToken);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requirements + "\n" + pythonVersion)))[..16].ToLowerInvariant();
        var venvPath = Path.Combine(cacheRoot, $"venv-{key}");
        var readyMarker = Path.Combine(venvPath, ".snapcd-ready");

        if (File.Exists(readyMarker))
            return venvPath;

        var gate = VenvLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(readyMarker))
                return venvPath;

            if (Directory.Exists(venvPath))
                Directory.Delete(venvPath, recursive: true);

            var requirementsFile = Path.Combine(cacheRoot, $"requirements-{key}.txt");
            await File.WriteAllTextAsync(requirementsFile, requirements, cancellationToken);

            await RunToolAsync(settings.PythonPath, ["-m", "venv", venvPath], cacheRoot, settings, cancellationToken);
            await RunToolAsync(Path.Combine(venvPath, "bin", "python"), ["-m", "pip", "install", "--quiet", "-r", requirementsFile], cacheRoot, settings, cancellationToken);

            await File.WriteAllTextAsync(readyMarker, "", cancellationToken);
            return venvPath;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task RunToolAsync(string fileName, string[] args, string workingDir, PolicyEvaluationSettings settings, CancellationToken cancellationToken)
    {
        await CaptureToolOutputAsync(fileName, args, workingDir, settings, cancellationToken);
    }

    private static async Task<string> CaptureToolOutputAsync(string fileName, string[] args, string workingDir, PolicyEvaluationSettings settings, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

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
                $"Failed to start '{fileName}': {e.Message}. The interpreter/tooling for CrossGuard packs is operator-provided.", e);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.MaterializeTimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new PolicyEvaluationException($"'{fileName} {string.Join(' ', args)}' timed out provisioning a CrossGuard pack environment");
        }

        if (process.ExitCode != 0)
            throw new PolicyEvaluationException($"'{fileName} {string.Join(' ', args)}' failed: {stderr}");

        return stdout.ToString();
    }

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
