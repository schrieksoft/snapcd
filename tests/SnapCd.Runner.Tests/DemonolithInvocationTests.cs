// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using System.Diagnostics;
using SnapCd.Runner.Services.SplitMonolith;
using SnapCd.Runner.Utils;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// The command the runner builds, run the way the runner runs it: through bash -c with only double
/// quotes escaped. The binary is operator-provided, so each test returns early where it is absent
/// rather than failing a machine that does not have it.
/// </summary>
public class DemonolithInvocationTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "demonolith-invoke-" + Guid.NewGuid().ToString("N"));

    public DemonolithInvocationTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public async Task A_Built_Command_Reaches_The_Binary()
    {
        if (!HasDemonolith()) return;

        var (exitCode, output) = await RunAsBaseEngineWould(DemonolithCommand.Build("--version", null, null));

        Assert.Equal(0, exitCode);
        Assert.Contains("demonolith", output);
    }

    /// <summary>
    /// A root directory with a space must arrive as one argument. Asserted against the unquoted
    /// form too, which demonolith rejects, so the test fails if the quoting is ever dropped.
    /// </summary>
    [Fact]
    public async Task A_Root_Directory_With_A_Space_Survives_Bash()
    {
        if (!HasDemonolith()) return;

        var root = Path.Combine(_dir, "my monolith");
        Directory.CreateDirectory(root);

        var built = DemonolithCommand.Build("split refactor map", root, null);
        var (exitCode, _) = await RunAsBaseEngineWould(built);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(root, "demonolith-refactor-map.yaml")), "the map was not written into the spaced directory");

        var (unquotedExit, unquotedOutput) = await RunAsBaseEngineWould(built.Replace($"\"{root}\"", root));

        Assert.NotEqual(0, unquotedExit);
        Assert.Contains("unknown command", unquotedOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task An_Unknown_Subcommand_Is_A_Non_Zero_Exit()
    {
        if (!HasDemonolith()) return;

        var (exitCode, _) = await RunAsBaseEngineWould(DemonolithCommand.Build("refactor nonsense", null, null));

        Assert.NotEqual(0, exitCode);
    }

    private static bool HasDemonolith()
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            if (!string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir, "demonolith"))) return true;
        }

        return false;
    }

    private async Task<(int ExitCode, string Output)> RunAsBaseEngineWould(string script)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{StringFormatting.EscapeBashScript(script)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _dir
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync() + await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, output);
    }
}
