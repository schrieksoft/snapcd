// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Services.SplitMonolith;

/// <summary>
/// Builds a demonolith invocation. The binary is expected on the runner and is not shipped with
/// Snap CD.
///
/// --output json is deliberately not passed: it replaces the human report on stdout, and that
/// report is what the runner streams into the job's logs. Statistics come from the receipts
/// demonolith writes, which carry a version the reader can check — the stdout document does not.
///
/// Sub-commands are invoked individually rather than the bare `migrate` pipeline, so the
/// confirmation it pauses for never arises and --yes is not passed — it belongs to that pipeline's
/// flag set alone. Snap CD's approval gate sits on the same boundary.
/// </summary>
public static class DemonolithCommand
{
    public static string Build(
        string subcommand,
        string? rootDirectory,
        string? engine,
        params string[] extraFlags)
    {
        var command = $"demonolith {subcommand}";

        if (!string.IsNullOrWhiteSpace(rootDirectory))
            command += $" --root-dir \"{rootDirectory}\"";

        // --exec-path is not offered: Snap CD does not let a user name a binary. A runner declares
        // additional paths to search, and the engine is chosen by name.
        if (!string.IsNullOrWhiteSpace(engine))
            command += $" --engine {engine.ToLowerInvariant()}";

        foreach (var flag in extraFlags.Where(f => !string.IsNullOrWhiteSpace(f)))
            command += $" {flag}";

        return command;
    }

    /// <summary>
    /// Backend settings that live outside the backend block, as demonolith expects them. Taken from
    /// the module's own BackendConfig array flags, the same source the Init step uses.
    /// </summary>
    public static IEnumerable<string> BackendConfigFlags(IEnumerable<string> backendConfigs) =>
        backendConfigs
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => $"--backend-config \"{c}\"");
}
