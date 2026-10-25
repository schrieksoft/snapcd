// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModuleHolds;

/// <summary>Holds the listed Modules, and the Modules in the listed Namespaces and Stacks, out of deployment, or keeps holding them, until the next heartbeat is due.</summary>
public class ModuleHoldHeartbeatRequestDto
{
    /// <summary>Every Module the client is holding; those not yet held are held now.</summary>
    public List<Guid> ModuleIds { get; set; } = [];

    /// <summary>Every Namespace the client is holding, which holds every Module in it; those not yet held are held now.</summary>
    public List<Guid> NamespaceIds { get; set; } = [];

    /// <summary>Every Stack the client is holding, which holds every Module in its Namespaces; those not yet held are held now.</summary>
    public List<Guid> StackIds { get; set; } = [];

    /// <summary>How often the client sends a heartbeat; when unset the server's default is used. Kept within the server's limits.</summary>
    public int? HeartbeatIntervalSeconds { get; set; }

    /// <summary>How long past the heartbeat interval a hold lasts; when unset the server's default is used. Kept within the server's limits.</summary>
    public int? HeartbeatBufferSeconds { get; set; }
}
