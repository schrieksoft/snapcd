// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using SnapCd.Runner.Services.ModuleSourceRefresher;

namespace SnapCd.Runner.Tests;

/// <summary>
/// The real snapcd-inspect binary, built once per test run from tools/snapcd-inspect. Needs Go on the PATH.
/// </summary>
public static class InspectBinary
{
    private static readonly Lazy<SnapCdInspect> Built = new(Build);

    public static SnapCdInspect Inspect => Built.Value;

    private static SnapCdInspect Build()
    {
        var repoRoot = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(repoRoot, "tools", "snapcd-inspect")))
            repoRoot = Path.GetDirectoryName(repoRoot) ?? throw new InvalidOperationException("tools/snapcd-inspect not found above the test directory");

        var output = Path.Combine(Path.GetTempPath(), "snapcd-inspect-tests", $"snapcd-inspect-{Guid.NewGuid():N}");
        var startInfo = new ProcessStartInfo("go", ["build", "-C", Path.Combine(repoRoot, "tools", "snapcd-inspect"), "-o", output, "."])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("could not start go");
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"go build of snapcd-inspect failed: {stderr}");

        return new SnapCdInspect(output);
    }
}
