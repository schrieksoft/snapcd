// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.StateFiles;

public class StateFileReadDto : StateFileCreateDto, IDto
{
    public Guid Id { get; set; }
    public string? LockId { get; set; }
    public string? LockInfo { get; set; }
    public DateTimeOffset? LockCreatedAt { get; set; }
    public Guid? LockedById { get; set; }
    public string? LockedByPrincipalDiscriminator { get; set; }
}
