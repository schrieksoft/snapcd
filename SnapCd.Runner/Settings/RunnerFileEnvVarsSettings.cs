// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Settings;

/// <summary>
/// Environment variables whose values are read from a file each time an engine process starts,
/// keyed by variable name with the file path as the value. For credentials that the platform
/// replaces as they expire, such as a projected ServiceAccount token.
/// </summary>
public class RunnerFileEnvVarsSettings : Dictionary<string, string>
{
}
