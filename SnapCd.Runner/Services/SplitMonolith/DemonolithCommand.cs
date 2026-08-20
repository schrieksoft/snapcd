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
/// Snap CD; --output json is always passed, since the runner reads the result rather than a person.
/// </summary>
public static class DemonolithCommand
{
    public static string Build(string subcommand, string? execPath, string? rootDirectory, string? engine)
    {
        var binary = string.IsNullOrWhiteSpace(execPath) ? "demonolith" : execPath;

        var command = $"{binary} {subcommand} --output json";

        if (!string.IsNullOrWhiteSpace(rootDirectory))
            command += $" --root-dir \"{rootDirectory}\"";

        if (!string.IsNullOrWhiteSpace(engine))
            command += $" --engine {engine.ToLowerInvariant()}";

        return command;
    }
}
