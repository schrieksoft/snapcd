// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;

/// <summary>Supply: this integration serves every module under the given stack.</summary>
public class IntegrationStackSupply : AuditBase, IEntity, IOrganizationChild, IIntegrationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid IntegrationId { get; set; }
    public Guid StackId { get; set; }

    [JsonIgnore] public Integration Integration { get; set; } = null!;
    [JsonIgnore] public Stack Stack { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId() => IntegrationId;
}
