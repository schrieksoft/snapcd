// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Host.Telemetry;

/// <summary>
/// The daily usage beacon to the Snap CD Licensing Service: installation seed, version, module count
/// and job count, nothing else. Disabling it also stops the version indicator and the What's New feed,
/// which arrive in the beacon's response.
/// </summary>
public class TelemetrySettings
{
    /// <summary>Whether the beacon runs at all. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Cron expression for the beacon. Defaults to 05:00 daily.</summary>
    public string Cron { get; set; } = "0 5 * * *";
}
