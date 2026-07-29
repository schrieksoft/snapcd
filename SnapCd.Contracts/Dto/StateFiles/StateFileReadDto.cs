// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.StateFiles;

/// <summary>DTO for StateFile responses (GET operations).</summary>
public class StateFileReadDto : StateFileCreateDto, IDto
{
    /// <summary>Unique ID of the State File.</summary>
    public Guid Id { get; set; }
    /// <summary>ID of the currently held lock, when locked.</summary>
    public string? LockId { get; set; }
    /// <summary>Lock metadata supplied by the locking client, when locked.</summary>
    public string? LockInfo { get; set; }
    /// <summary>When the current lock was acquired, when locked.</summary>
    public DateTimeOffset? LockCreatedAt { get; set; }
    /// <summary>ID of the principal holding the lock, when locked.</summary>
    public Guid? LockedById { get; set; }
    /// <summary>Whether the lock holder is a User or a Service Principal, when locked.</summary>
    public string? LockedByPrincipalDiscriminator { get; set; }
}
