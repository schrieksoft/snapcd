// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class StackRoleAssignmentCreateDto
{

    /// <summary>ID of the Stack on which the role applies.</summary>
    public Guid StackId { get; set; }

    /// <summary>ID of the Principal to which the role is assigned.</summary>
    public Guid PrincipalId { get; set; }

    /// <summary>Type of Principal that the `principal_id` identifies. Must be one of 'User', 'ServicePrincipal' and 'Group'</summary>
    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }

    /// <summary>Name of the Role that is assigned.</summary>
    public StackRole RoleName { get; set; }
}
