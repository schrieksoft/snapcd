// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModulePulumiInlinePolicies;

/// <summary>Name and ID of a Module Pulumi Inline Policy, for a principal who may identify it without reading it.</summary>
public class ModulePulumiInlinePolicyMetadataReadDto : IDto
{
    /// <summary>Unique ID of the Module Pulumi Inline Policy.</summary>
    public Guid Id { get; set; }

    /// <summary>Name of the Module Pulumi Inline Policy.</summary>
    public string Name { get; set; } = null!;

    /// <summary>ID of the Module this Module Pulumi Inline Policy belongs to.</summary>
    public Guid ModuleId { get; set; }
}
