// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Agents;

/// <summary>
/// DTO for creating a new Agent (POST operations).
/// </summary>
public class AgentCreateDto
{
    /// <summary>ID of the Service Principal that the Agent authenticates as.</summary>
    public Guid ServicePrincipalId { get; set; }

    /// <summary>Unique name of the Agent.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Indicates whether or not the Agent is disabled.</summary>
    public bool IsDisabled { get; set; }

    /// <summary>Setting this to 'true' allows you to connect multiple instances of this Agent simultaneously.</summary>
    public bool AllowMultipleInstances { get; set; }

    /// <summary>Supplies this Agent to every Module in the organization, without requiring per-scope supplies.</summary>
    public bool IsSuppliedToAllModules { get; set; }
}
