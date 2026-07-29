// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Missions;

/// <summary>
/// DTO for creating a new ModuleMission (POST operations).
/// Fires for events for the targeted module.
/// </summary>
public class ModuleMissionCreateDto
{
    /// <summary>ID of the Agent that runs this Mission.</summary>
    public Guid AgentId { get; set; }

    /// <summary>ID of the Module this Mission is scoped to.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>Which named mission definition this row references.</summary>
    public MissionType MissionType { get; set; }

    /// <summary>Optional named-sidecar override sent to the agent at dispatch. When unset (null), the agent invokes its only registered sidecar; the run fails if the agent has zero or multiple sidecars and no name was supplied.</summary>
    public string? SidecarName { get; set; }

    /// <summary>Indicates whether or not the Mission is disabled.</summary>
    public bool IsDisabled { get; set; }
}
