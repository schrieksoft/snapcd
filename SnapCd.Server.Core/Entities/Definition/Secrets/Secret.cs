// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Utils.Vaults;

namespace SnapCd.Server.Core.Entities.Definition.Secrets;

public class Secret : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    public virtual SecretScope ScopeKind { get; init; }

    /// <summary>
    /// Whether this Secret's value is stored under a name carrying its owner's id as well as its
    /// own. A scoped name is what lets a deleted Stack, Namespace or Module have its values found
    /// by prefix. False until the sweep has moved it; new Secrets are written scoped.
    /// </summary>
    public bool NameContainsScope { get; set; } = true;

    /// <summary>
    /// When the value was first found to be absent from the vault under either name. Null while
    /// the value is there. Advisory: nothing reads it to decide whether the Secret works, so a
    /// stale value misleads rather than breaks.
    /// </summary>
    public DateTime? RemoteValueMissingSince { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    // Injected factories
    public virtual Guid ParentId()
    {
        return OrganizationId;
    }
}