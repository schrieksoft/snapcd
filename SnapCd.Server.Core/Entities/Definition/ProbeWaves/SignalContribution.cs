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
/// One edge a signal could travel along, from an upstream participant to a consumer that reads
/// its outputs. Two of these on one consumer is what makes a diamond expressible, and the
/// consumer is released only when none of them is still Potential.
///
/// The only status in a wave, and it carries one because an upstream resolves differently for
/// different consumers: one may read a changed output while another does not. That answer exists
/// nowhere else.
/// </summary>
public class SignalContribution : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The participant that may need to probe.</summary>
    public Guid ProbeWaveParticipantId { get; set; }

    /// <summary>The participant on whose account it may need to.</summary>
    public Guid UpstreamParticipantId { get; set; }

    public SignalContributionStatus Status { get; set; }

    [JsonIgnore] public ProbeWaveParticipant ProbeWaveParticipant { get; set; } = null!;
    [JsonIgnore] public ProbeWaveParticipant UpstreamParticipant { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ProbeWaveParticipantId;
    }
}
