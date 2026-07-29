// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.NamespaceExtraFiles;

/// <summary>DTO for creating a new NamespaceExtraFile (POST operations).</summary>
public class NamespaceExtraFileCreateDto
{
    /// <summary>ID of the Namespace Extra File's parent Namespace.</summary>
    public Guid NamespaceId { get; set; }

    /// <summary>Name of the Namespace Extra File. This name will be use as the name of the file that is created. Must be unique in combination with `namespace_id`.</summary>
    public string FileName { get; set; } = null!;

    /// <summary>Contents of the Namespace Extra File</summary>
    public string Contents { get; set; } = null!;

    /// <summary>If true any pre-existing file with the same name will be overwritten.</summary>
    public bool Overwrite { get; set; }
}
