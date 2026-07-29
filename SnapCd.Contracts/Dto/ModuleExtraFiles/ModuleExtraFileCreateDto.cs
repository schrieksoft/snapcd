// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModuleExtraFiles;

/// <summary>
/// DTO for creating a new ModuleExtraFile (POST operations).
/// </summary>
public class ModuleExtraFileCreateDto
{
    /// <summary>ID of the Module Extra File's parent Module.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>Name of the Module Extra File. This name will be use as the name of the file that is created. Must be unique in combination with `module_id`.</summary>
    public string FileName { get; set; } = null!;

    /// <summary>Contents of the Module Extra File</summary>
    public string Contents { get; set; } = null!;

    /// <summary>If true any pre-existing file with the same name will be overwritten.</summary>
    public bool? Overwrite { get; set; }
}
