// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// A user's starred item. Personal (per user) and org-scoped. The target is referenced by
/// TargetType + TargetId without a foreign key: cascade FKs to Stack/Namespace/Module would create
/// multiple cascade paths (rejected by SQL Server), so favorites whose target has been deleted are
/// simply filtered out at read time when resolving through the secured target queries.
/// </summary>
public class UserFavorite : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    public FavoriteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
