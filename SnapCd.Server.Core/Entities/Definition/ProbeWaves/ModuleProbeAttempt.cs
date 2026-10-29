// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.ProbeWaves;

/// <summary>
/// What one Module did in one wave: the probe job that ran for it, and the signals that probe
/// reported.
///
/// Written when the Module first does something rather than in advance, so a row here means a
/// probe was attempted. Which Modules a wave might still reach is a question about contributions,
/// which hang off the wave in their own right and do not refer to this table.
///
/// No status column, because nothing is left for one to say. Whether it is running, faulted or
/// finished is the probe job's own status; whether it signalled is whether a set exists.
/// </summary>
public class ModuleProbeAttempt : AuditBase, IEntity, IModuleChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ProbeWaveId { get; set; }
    public Guid ModuleId { get; set; }

    /// <summary>Unix milliseconds, which is what orders a Module's attempts within a wave.</summary>
    public long Timestamp { get; set; }

    /// <summary>What this Module reported, once its probe has run.</summary>
    public Guid? SignalOutputSetId { get; set; }

    /// <summary>
    /// What this Module predicted it would do to its own resources. Null where the probe reported
    /// no resource changes at all, and where the reporting engine does not report them.
    /// </summary>
    public Guid? ResourceChangeSetId { get; set; }

    /// <summary>The job carrying the probe, written before it starts.</summary>
    public Guid? ProbeModuleJobId { get; set; }

    [JsonIgnore] public ProbeWave ProbeWave { get; set; } = null!;
    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public SignalOutputSet? SignalOutputSet { get; set; }
    [JsonIgnore] public ResourceChangeSet? ResourceChangeSet { get; set; }
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
