// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.ProbeWaves;

/// <summary>
/// One propagation episode: a Module's plan changed its outputs, and this is the attempt to say
/// what that does downstream.
///
/// No status column. Whether a wave is still going is whether any participant has an unresolved
/// contribution or a running job, so a column here would restate what those rows already say.
/// </summary>
public class ProbeWave : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The Module whose outputs changed.</summary>
    public Guid OriginModuleId { get; set; }

    /// <summary>The job whose plan reported the change.</summary>
    public Guid OriginModuleJobId { get; set; }

    /// <summary>
    /// Copied from settings at creation, so an operator editing the setting does not change a wave
    /// already in flight.
    /// </summary>
    public int MaxDepth { get; set; }

    public List<ProbeWaveParticipant> Participants { get; set; } = new();

    [JsonIgnore] public Module OriginModule { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
