// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.StateFiles;

/// <summary>DTO for creating a new StateFile (POST operations).</summary>
public class StateFileCreateDto
{
    /// <summary>ID of the State Store the State File belongs to.</summary>
    public Guid StateStoreId { get; set; }
    /// <summary>Name of the State File. Must be unique within the State Store.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The state document content, as a JSON string.</summary>
    public string? Data { get; set; }
}
