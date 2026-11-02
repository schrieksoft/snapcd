// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.OutputSetModuleSupplies;

/// <summary>
/// DTO for creating a new OutputSetModuleSupply (POST operations).
/// </summary>
public class OutputSetModuleSupplyCreateDto
{
    /// <summary>ID of the Module whose Output Set is supplied.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>ID of the Module the Output Set is supplied to. Must be unique in combination with `module_id`.</summary>
    public Guid ConsumingModuleId { get; set; }
}
