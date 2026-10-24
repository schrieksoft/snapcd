// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModuleHolds;

/// <summary>The holds as the server applied them.</summary>
public class ModuleHoldHeartbeatResponseDto
{
    /// <summary>The interval the client must send heartbeats at, after the server's limits are applied.</summary>
    public required int EffectiveHeartbeatIntervalSeconds { get; set; }

    /// <summary>How long each hold lasts after this heartbeat, after the server's limits are applied.</summary>
    public required int EffectiveExpirySeconds { get; set; }

    /// <summary>Set when the server's limits changed the requested heartbeat interval or buffer, saying what was applied and why.</summary>
    public string? Warning { get; set; }

    /// <summary>Modules that could not be held: the caller may not update them, or another principal holds them.</summary>
    public List<Guid> NotHeldModuleIds { get; set; } = [];

    /// <summary>Namespaces that could not be held: the caller may not update them, or another principal holds them.</summary>
    public List<Guid> NotHeldNamespaceIds { get; set; } = [];

    /// <summary>Stacks that could not be held: the caller may not update them, or another principal holds them.</summary>
    public List<Guid> NotHeldStackIds { get; set; } = [];
}
