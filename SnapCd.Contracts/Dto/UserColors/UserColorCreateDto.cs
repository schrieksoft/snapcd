// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.UserColors;

public class UserColorCreateDto
{
    public ColorTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    /// <summary>Hex colour, e.g. "#E85D1A". Null or empty clears the colour.</summary>
    public string? Color { get; set; }
}
