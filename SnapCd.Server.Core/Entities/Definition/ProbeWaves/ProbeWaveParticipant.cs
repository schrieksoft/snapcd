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
/// One Module the wave's origin can reach, including the origin itself at the start of the chain.
/// The unit of work: a Module probes once per wave with every contribution merged.
///
/// No status column, because nothing is left for one to say. Whether it is running, faulted or
/// finished is the probe job's own status; whether it signalled is whether a set exists; whether
/// it had anything to do is its contributions. A column here would mirror a ModuleJob and could
/// disagree with it.
/// </summary>
public class ProbeWaveParticipant : AuditBase, IEntity, IModuleChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ProbeWaveId { get; set; }
    public Guid ModuleId { get; set; }

    /// <summary>What this Module reported, once it has probed.</summary>
    public Guid? SignalOutputSetId { get; set; }

    /// <summary>
    /// The latest probe attempt, overwritten on a retry. Not a claim: the job's own status says
    /// whether it is running. It is how the origin reaches a consumer's probe logs, which the
    /// signal set cannot do while a probe runs or after it failed.
    /// </summary>
    public Guid? LastProbeModuleJobId { get; set; }

    /// <summary>Contributions this Module waits on.</summary>
    public List<SignalContribution> Contributions { get; set; } = new();

    [JsonIgnore] public ProbeWave ProbeWave { get; set; } = null!;
    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public SignalOutputSet? SignalOutputSet { get; set; }
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
