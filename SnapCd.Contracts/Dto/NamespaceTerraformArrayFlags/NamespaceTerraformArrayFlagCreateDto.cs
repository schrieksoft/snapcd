// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;

/// <summary>DTO for creating a new NamespaceTerraformArrayFlag (POST operations).</summary>
public class NamespaceTerraformArrayFlagCreateDto
{
    /// <summary>The command task this flag applies to.</summary>
    public TerraformCommandTask Task { get; set; }

    /// <summary>The Terraform CLI array flag name.</summary>
    public TerraformArrayFlag Flag { get; set; }

    /// <summary>The value for the flag.</summary>
    [Required] [MaxLength(1000)] public string Value { get; set; } = null!;

    /// <summary>ID of the parent Namespace.</summary>
    public Guid NamespaceId { get; set; }
}
