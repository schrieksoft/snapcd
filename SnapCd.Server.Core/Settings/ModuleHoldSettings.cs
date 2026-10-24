// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// The defaults and limits for a module hold's timing. A client's heartbeat interval and buffer, or the defaults where it
/// sends none, are each clamped to their limits; the hold lasts the interval plus the buffer after each heartbeat.
/// </summary>
public class ModuleHoldSettings
{
    public int DefaultHeartbeatIntervalSeconds { get; set; } = 10;

    public int MinHeartbeatIntervalSeconds { get; set; } = 5;

    public int MaxHeartbeatIntervalSeconds { get; set; } = 30;

    public int DefaultHeartbeatBufferSeconds { get; set; } = 10;

    /// <summary>At least 1 second applies whatever is set, so a heartbeat always arrives before the hold lapses.</summary>
    public int MinHeartbeatBufferSeconds { get; set; } = 1;

    public int MaxHeartbeatBufferSeconds { get; set; } = 30;
}
