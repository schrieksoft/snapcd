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
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One address a job touched, and what became of it. Every job that moves or observes state writes
/// these, so "what happened to this resource" has one answer across job types.
///
/// Why an address failed is in the job's logs. The row records that it did, which is what makes a
/// job partially completed and says which addresses to try again.
/// </summary>
public class ManualModuleJobAddress : AuditBase, IEntity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid JobId { get; set; }

    public Guid ModuleId { get; set; }

    /// <summary>The resource address, as the state names it.</summary>
    [MaxLength(500)] public string Address { get; set; } = null!;

    public AddressOperation Operation { get; set; }

    /// <summary>
    /// The other half of the operation: an mv's destination address, or an import's resource id.
    /// Null for the operations that name one address and nothing else.
    /// </summary>
    [MaxLength(500)] public string? Target { get; set; }

    public AddressOutcome Outcome { get; set; }

    /// <summary>When the job reported this address. Per-address timings are not measured.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    [JsonIgnore] public ManualModuleJob Job { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId() => JobId;
}
