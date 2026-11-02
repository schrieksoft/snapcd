// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.OutputNamespaceSupplies;

/// <summary>
/// DTO for creating a new OutputNamespaceSupply (POST operations).
/// </summary>
public class OutputNamespaceSupplyCreateDto
{
    /// <summary>ID of the Module that produces the Output.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>Name of the Output being supplied.</summary>
    public string OutputName { get; set; } = null!;

    /// <summary>ID of the Namespace the Output is supplied to, reaching every Module in it. Must be unique in combination with `module_id` and `output_name`.</summary>
    public Guid ConsumingNamespaceId { get; set; }
}
