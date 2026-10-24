// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModuleHolds;

/// <summary>How a client holding Modules sends heartbeats. Either value left unset takes the server's default.</summary>
public class ModuleHoldTimingDto
{
    /// <summary>How often the client sends a heartbeat. Kept within the server's limits.</summary>
    public int? HeartbeatIntervalSeconds { get; set; }

    /// <summary>How long past the heartbeat interval a hold lasts. Kept within the server's limits.</summary>
    public int? HeartbeatBufferSeconds { get; set; }
}
