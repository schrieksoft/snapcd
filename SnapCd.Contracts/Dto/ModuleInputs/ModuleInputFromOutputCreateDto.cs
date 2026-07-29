// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleInputs.Base;

namespace SnapCd.Contracts.Dto.ModuleInputs;

/// <summary>DTO for creating a new ModuleInputFromOutput (POST operations).</summary>
public class ModuleInputFromOutputCreateDto : ModuleInputCreateDto
{
    /// <summary>ID of the Module from which to take the Output.</summary>
    public Guid OutputModuleId { get; set; }

    /// <summary>Name of Output to take as input.</summary>
    public string OutputName { get; set; } = null!;
}
