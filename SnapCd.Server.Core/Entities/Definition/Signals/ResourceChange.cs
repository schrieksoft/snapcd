// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.Signals;

/// <summary>
/// One resource a probe predicted a change to. The address carries the module path, so a change
/// inside a child module stays attributable.
///
/// No attribute values: a resource's attributes are large and often secret, where an output's
/// value is small and has a sensitive path of its own. What an approver reads is the verb and
/// what it applies to.
/// </summary>
public class ResourceChange : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ResourceChangeSetId { get; set; }

    /// <summary>The resource's address as the engine prints it, module path included.</summary>
    [MaxLength(1024)] public string Address { get; set; } = null!;

    [MaxLength(255)] public string ResourceType { get; set; } = null!;

    [MaxLength(255)] public string Name { get; set; } = null!;

    /// <summary>
    /// The plan's verdict, in the engine's own wire form. A replace carries two verbs, so this is
    /// the list as written rather than one value: "delete,create" and "create,delete" differ in
    /// whether the new object is made before the old one goes.
    /// </summary>
    [MaxLength(100)] public string Actions { get; set; } = null!;

    [JsonIgnore] public ResourceChangeSet ResourceChangeSet { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ResourceChangeSetId;
    }
}
