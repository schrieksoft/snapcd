// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Variables;

namespace SnapCd.Contracts.Dto.VariableSets;

/// <summary>
/// DTO for creating a new VariableSet (POST operations).
/// </summary>
public class VariableSetCreateDto
{
    public Guid ModuleId { get; set; }
    public long Timestamp { get; set; }

    public string Checksum { get; set; } = null!;
    public List<VariableCreateDto>? Variables { get; set; }
}
