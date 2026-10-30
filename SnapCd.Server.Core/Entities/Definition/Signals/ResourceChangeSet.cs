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
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.Signals;

/// <summary>
/// What one probe predicted this Module would do to its own resources, as opposed to
/// <see cref="SignalOutputSet"/>, which is what it would pass downstream. A Module can change in
/// place while its outputs stay put, so the two sets answer different questions and a reader of an
/// approval needs both.
/// </summary>
public class ResourceChangeSet : AuditBase, IEntity, IModuleChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ModuleId { get; set; }

    /// <summary>The wave this was produced in.</summary>
    public Guid ProbeWaveId { get; set; }

    /// <summary>The job whose probe step wrote it.</summary>
    public Guid ModuleJobId { get; set; }

    public long Timestamp { get; set; }

    /// <summary>
    /// How many changes the probe predicted, which is not necessarily how many rows are stored:
    /// a probe predicting more than the cap keeps the count and a truncated list.
    /// </summary>
    public int TotalCount { get; set; }

    public List<ResourceChange> ResourceChanges { get; set; } = new();

    [JsonIgnore] public ProbeWave ProbeWave { get; set; } = null!;
    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
