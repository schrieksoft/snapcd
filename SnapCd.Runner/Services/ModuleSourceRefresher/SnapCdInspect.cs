// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services.ModuleSourceRefresher;

/// <summary>
/// Runs the snapcd-inspect binary against a bare clone to resolve, per watched root, the transitive closure of
/// locally-referenced terraform directories at a commit. Discovery is exact for module topology because
/// terraform requires module source arguments to be literal strings.
///
/// The binary is carried as an embedded resource inside the Runner itself (added at publish time by CI) and
/// extracted to the working directory on first use, so no separate install is needed on any distribution
/// channel. When no resource is embedded (local dev builds) or the platform cannot run it, resolution falls
/// back to <see cref="SourceCacheSettings.InspectBinaryPath"/> — a non-default value there always wins.
/// </summary>
public class SnapCdInspect
{
    private const string EmbeddedResourceName = "SnapCd.Runner.EmbeddedTools.snapcd-inspect";
    private const string DefaultBinaryName = "snapcd-inspect";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Lazy<string> _binaryPath;

    public SnapCdInspect(
        IOptions<SourceCacheSettings> settings,
        IOptions<WorkingDirectorySettings> workingDirectorySettings,
        ILogger<SnapCdInspect> logger)
    {
        _binaryPath = new Lazy<string>(() => ResolveBinary(settings.Value.InspectBinaryPath, workingDirectorySettings.Value.WorkingDirectory, logger));
    }

    public SnapCdInspect(string binaryPath)
    {
        _binaryPath = new Lazy<string>(() => binaryPath);
    }

    private static string ResolveBinary(string configuredPath, string workingDirectory, ILogger logger)
    {
        // An explicitly configured path is operator intent — never override it with the embedded binary.
        if (configuredPath != DefaultBinaryName)
            return configuredPath;

        if (!OperatingSystem.IsLinux())
            return configuredPath;

        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedResourceName);
        if (resource == null)
        {
            logger.LogDebug("No embedded snapcd-inspect binary in this build; resolving {Binary} from PATH", configuredPath);
            return configuredPath;
        }

        try
        {
            using var memory = new MemoryStream();
            resource.CopyTo(memory);
            var embedded = memory.ToArray();

            var targetDir = Path.Combine(workingDirectory, "tools");
            var targetPath = Path.Combine(targetDir, DefaultBinaryName);

            if (!File.Exists(targetPath) || !File.ReadAllBytes(targetPath).AsSpan().SequenceEqual(embedded))
            {
                Directory.CreateDirectory(targetDir);
                var tempPath = targetPath + "." + Guid.NewGuid().ToString("N")[..8];
                File.WriteAllBytes(tempPath, embedded);
                File.SetUnixFileMode(tempPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                File.Move(tempPath, targetPath, true);
                logger.LogInformation("Extracted embedded snapcd-inspect to {TargetPath}", targetPath);
            }

            return targetPath;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract embedded snapcd-inspect; resolving {Binary} from PATH", configuredPath);
            return configuredPath;
        }
    }

    private class InspectResult
    {
        public List<InspectClosure> Closures { get; set; } = new();
    }

    private class InspectClosure
    {
        public string RootPath { get; set; } = null!;
        public List<string> ReferencedPaths { get; set; } = new();
        public List<string> DanglingPaths { get; set; } = new();
    }

    public List<ModuleClosure> Discover(string cloneDir, string commitSha, IReadOnlyCollection<string> roots)
    {
        var binaryPath = _binaryPath.Value;

        var startInfo = new ProcessStartInfo
        {
            FileName = binaryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--repo");
        startInfo.ArgumentList.Add(cloneDir);
        startInfo.ArgumentList.Add("--commit");
        startInfo.ArgumentList.Add(commitSha);
        startInfo.ArgumentList.Add("--roots");
        startInfo.ArgumentList.Add(string.Join(',', roots));

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"{binaryPath} failed with exit code {process.ExitCode}: {stderr}");

        var result = JsonSerializer.Deserialize<InspectResult>(stdout, JsonOptions)
                     ?? throw new Exception($"{binaryPath} produced no parseable output");

        return result.Closures
            .Select(c => new ModuleClosure
            {
                RootPath = c.RootPath,
                ReferencedPaths = c.ReferencedPaths
            })
            .ToList();
    }
}
