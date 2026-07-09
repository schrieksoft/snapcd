// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Runner.Settings;

/// <summary>
/// Coordinates of the Snap CD Server the Runner connects to.
/// </summary>
public class ServerSettings
{
    /// <summary>
    /// Base URL of the Snap CD Server, including scheme and port. The Runner opens its SignalR
    /// connection to {Url}/runnerhub and obtains JWTs from {Url}/connect/token.
    /// </summary>
    [Required]
    public string Url { get; set; } = null!;
}
