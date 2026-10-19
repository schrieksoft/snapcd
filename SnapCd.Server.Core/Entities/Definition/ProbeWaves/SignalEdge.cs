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

namespace SnapCd.Server.Core.Entities.Definition.ProbeWaves;

/// <summary>
/// One edge a signal could travel along, from a Module that changed an output to one that reads
/// it. Two of these on one consumer is what makes a diamond expressible, and the consumer probes
/// only once none of its edges is still Potential.
///
/// Named by Module rather than by a row in another table: two upstreams reaching one consumer
/// write two rows that share nothing, so neither has to exist before the other can be written.
///
/// The only status in a wave, and it carries one because an upstream resolves differently for
/// different consumers: one may read a changed output while another does not. That answer exists
/// nowhere else.
/// </summary>
public class SignalEdge : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ProbeWaveId { get; set; }

    /// <summary>The Module that may need to probe.</summary>
    public Guid ConsumerModuleId { get; set; }

    /// <summary>The Module on whose account it may need to.</summary>
    public Guid UpstreamModuleId { get; set; }

    public SignalEdgeStatus Status { get; set; }

    [JsonIgnore] public ProbeWave ProbeWave { get; set; } = null!;
    [JsonIgnore] public Module ConsumerModule { get; set; } = null!;
    [JsonIgnore] public Module UpstreamModule { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ProbeWaveId;
    }
}
