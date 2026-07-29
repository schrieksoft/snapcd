// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.GroupMembers.Base;

public class GroupMemberCreateDto
{
    /// <summary>ID of the Group to assign membership of.</summary>
    public Guid GroupId { get; set; }

    /// <summary>ID of the Principal to assign to the Group.</summary>
    public Guid PrincipalId { get; set; }

    /// <summary>Type of Principal to assign to the Group. Must be one of 'User' and 'ServicePrincipal'</summary>
    public GroupMemberDiscriminator GroupMemberDiscriminator { get; set; }
}
