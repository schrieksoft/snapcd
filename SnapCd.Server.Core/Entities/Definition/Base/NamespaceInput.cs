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
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.Base;

public class NamespaceInput : AuditBase, IEntity, INamespaceChild, INamespaceInput
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid NamespaceId { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;

    public virtual InputKind InputKind { get; init; }

    public NamespaceInputUsageMode UsageMode { get; set; }


    [JsonIgnore] // So that JSON Serialization does not create a loop
    public virtual Organization Organization { get; set; } = null!;

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public Namespace Namespace { get; set; } = null!;

    public Guid ParentId()
    {
        return NamespaceId;
    }
}