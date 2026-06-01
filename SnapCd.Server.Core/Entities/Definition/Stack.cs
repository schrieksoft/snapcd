// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Stack : AuditBase, IEntity, IOrganizationChild, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public List<Namespace> Namespaces { get; set; } = null!;

    public List<RunnerStackAssignment>? RunnerStackAssignments { get; set; } = new();

    public List<StackSecret> SecretsScopedToStack { get; set; } = null!;

    // NEW Role Assignment navigation properties
    public virtual ICollection<UserStackRoleAssignment> UserStackRoleAssignments { get; set; } = new List<UserStackRoleAssignment>();
    public virtual ICollection<ServicePrincipalStackRoleAssignment> ServicePrincipalStackRoleAssignments { get; set; } = new List<ServicePrincipalStackRoleAssignment>();
    public virtual ICollection<GroupStackRoleAssignment> GroupStackRoleAssignments { get; set; } = new List<GroupStackRoleAssignment>();

    public StackTriggerBehaviour? TriggerBehaviourOnModified { get; set; } = StackTriggerBehaviour.DoNotTrigger;

    public virtual ICollection<StackRoleAssignment> StackRoleAssignments { get; set; } = new List<StackRoleAssignment>();

    public Guid ParentId()
    {
        return OrganizationId;
    }
}