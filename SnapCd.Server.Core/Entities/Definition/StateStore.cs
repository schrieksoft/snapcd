// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class StateStore : AuditBase, IEntity, IOrganizationChild, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;
    public List<StateFile> StateFiles { get; set; } = null!;

    public virtual ICollection<UserStateStoreRoleAssignment> UserStateStoreRoleAssignments { get; set; } = new List<UserStateStoreRoleAssignment>();
    public virtual ICollection<ServicePrincipalStateStoreRoleAssignment> ServicePrincipalStateStoreRoleAssignments { get; set; } = new List<ServicePrincipalStateStoreRoleAssignment>();
    public virtual ICollection<GroupStateStoreRoleAssignment> GroupStateStoreRoleAssignments { get; set; } = new List<GroupStateStoreRoleAssignment>();

    public virtual ICollection<StateStoreRoleAssignment> StateStoreRoleAssignments { get; set; } = new List<StateStoreRoleAssignment>();

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
