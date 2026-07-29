// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.ModulePulumiFlags;

/// <summary>DTO for creating a new ModulePulumiFlag (POST operations).</summary>
public class ModulePulumiFlagCreateDto
{
    /// <summary>The command task this flag applies to.</summary>
    public PulumiCommandTask Task { get; set; }

    /// <summary>The Pulumi CLI flag name.</summary>
    public PulumiFlag Flag { get; set; }

    /// <summary>The value for the flag. Optional for boolean flags.</summary>
    [MaxLength(1000)] public string? Value { get; set; }

    /// <summary>ID of the parent Module.</summary>
    public Guid ModuleId { get; set; }
}
