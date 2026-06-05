// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Runner.Utils;

namespace SnapCd.Runner.Settings;

/// <summary>
/// Discovery hints for the engine binaries (terraform, tofu, pulumi) the
/// Runner invokes per Job. The Runner looks for binaries on PATH first; entries here
/// extend that search.
/// </summary>
public class EngineSettings
{
    private List<string> _additionalBinaryPaths = new();

    /// <summary>
    /// Extra directories prepended to the Runner's binary-search path. Supports leading ~
    /// expansion. Useful when an engine ships in a non-standard location — for example
    /// ~/.pulumi/bin for a per-user Pulumi install.
    /// </summary>
    public List<string> AdditionalBinaryPaths
    {
        get => _additionalBinaryPaths;
        set => _additionalBinaryPaths = value.Select(PathUtils.ExpandTilde).ToList();
    }
}
