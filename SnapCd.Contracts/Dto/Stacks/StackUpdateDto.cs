// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Stacks;

/// <summary>
/// DTO for updating an existing Stack (PUT operations).
/// Includes Id to identify which stack to update.
/// </summary>
public class StackUpdateDto : StackCreateDto, IUpdateDto
{
    /// <summary>Unique ID of the Stack.</summary>
    public Guid Id { get; set; }
}
