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
/// One output's predicted change. The value object planwalker emits is stored whole, because a
/// consumer's inputs document carries it verbatim: only the key changes, from this output's name
/// to the variable it feeds. Splitting it into fields would mean reassembling it and trusting the
/// round trip.
/// </summary>
public class SignalOutput : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid SignalOutputSetId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    /// <summary>
    /// Lifted out of the stored object because it decides which subclass holds the value, and
    /// reading it from inside would mean parsing the object to decide where to put the object.
    /// </summary>
    public bool Sensitive { get; set; }

    [JsonIgnore] public SignalOutputSet SignalOutputSet { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return SignalOutputSetId;
    }
}
