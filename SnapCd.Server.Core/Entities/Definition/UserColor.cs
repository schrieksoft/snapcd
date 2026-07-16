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
/// A colour a user has assigned to a Stack, Namespace or Module. Personal (per user) and
/// org-scoped: it is a private visual aid — most usefully for picking a namespace's modules
/// out of a dependency graph that spans many of them — and is never visible to other users.
///
/// The target is referenced by TargetType + TargetId without a foreign key, for the same
/// reason as <see cref="UserFavorite"/>: cascade FKs to Stack/Namespace/Module would create
/// multiple cascade paths (rejected by SQL Server), so colours whose target has been deleted
/// are simply filtered out at read time.
///
/// One row per (user, target) — enforced by a unique index. Assigning a colour is therefore
/// an upsert rather than an insert, which is the one place this differs from UserFavorite
/// (a favourite is a toggle; a colour gets changed).
/// </summary>
public class UserColor : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    public ColorTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    /// <summary>
    /// The colour, as a hex string (e.g. "#E85D1A"). Validated on write; stored as given.
    /// </summary>
    public string Color { get; set; } = null!;

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
