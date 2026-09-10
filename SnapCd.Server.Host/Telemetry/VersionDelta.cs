// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Telemetry;

namespace SnapCd.Server.Host.Telemetry;

/// <summary>Names how far behind the running version is, or null when it is not behind at all.</summary>
public static class VersionDelta
{
    public static string? Describe(string? running, string? latest)
    {
        if (!SemanticVersion.TryParse(running, out var r) || !SemanticVersion.TryParse(latest, out var l) || r >= l) return null;
        return l.Major > r.Major ? "major" : l.Minor > r.Minor ? "minor" : "patch";
    }
}
