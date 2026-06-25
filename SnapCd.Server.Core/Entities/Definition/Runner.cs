// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Runner : AuditBase, IEntity, IOrganizationChild, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ServicePrincipalId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    public bool IsDisabled { get; set; }

    public bool AllowMultipleInstances { get; set; }

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual ServicePrincipal ServicePrincipal { get; set; } = null!;

    public List<Module> Modules { get; set; } = null!;

    public List<RunnerModuleSupply> RunnerModuleSupplies { get; set; } = null!;

    public List<RunnerNamespaceSupply> RunnerNamespaceSupplies { get; set; } = null!;

    public List<RunnerStackSupply> RunnerStackSupplies { get; set; } = null!;

    public List<SourceRefresherPreselection> SourceRefresherPreselections { get; set; } = null!;

    public List<RunnerRoleAssignment> RunnerRoleAssignments { get; set; } = null!;

    public bool IsSuppliedToAllModules { get; set; }

    public Guid ParentId()
    {
        return OrganizationId;
    }
}