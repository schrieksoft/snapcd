// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Integration.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// An outbound integration target (Slack, …). The row holds only identity/routing — the connection
/// (credentials + config) is serialized as one blob in the secret backend at
/// <c>integration--{organizationId}--{id}</c>, never on this table. Unique per org on
/// <c>(OrganizationId, IntegrationType, Name)</c>.
/// </summary>
public class Integration : AuditBase, IEntity, IOrganizationChild, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    [MaxLength(200)] public string Name { get; set; } = null!;

    public IntegrationType IntegrationType { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>Supply: when true this integration serves every module in the org (no per-scope assignment
    /// needed). Mirrors <c>Agent.IsSuppliedToAllModules</c>; the only mechanism for org-wide supply.</summary>
    public bool IsSuppliedToAllModules { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    [JsonIgnore] public List<IntegrationStackSupply> StackAssignments { get; set; } = null!;
    [JsonIgnore] public List<IntegrationNamespaceSupply> NamespaceAssignments { get; set; } = null!;
    [JsonIgnore] public List<IntegrationModuleSupply> ModuleAssignments { get; set; } = null!;
    [JsonIgnore] public List<IntegrationRoleAssignment> IntegrationRoleAssignments { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
