// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views;

/// <summary>
/// One wave as a reader sees it: where it started, who is taking part, and what each has predicted
/// so far.
/// </summary>
public class ProbeWaveView
{
    public Guid Id { get; set; }
    public Guid OriginModuleId { get; set; }
    public string OriginModuleName { get; set; } = null!;

    /// <summary>The job whose plan produced the signals this wave started from.</summary>
    public Guid OriginModuleJobId { get; set; }

    public int MaxDepth { get; set; }
    public DateTime CreatedDateTime { get; set; }

    public List<ProbeWaveParticipantView> Participants { get; set; } = [];

    /// <summary>
    /// Participants left out because the reader cannot see their Module. Counted rather than
    /// hidden: a wave that silently looks complete is worse than one that says what is missing.
    /// </summary>
    public int ParticipantsOmitted { get; set; }

    /// <summary>A wave is still going while any participant is still waiting on something.</summary>
    public bool IsSettled => Participants.All(p => !p.IsWaiting);
}

public class ProbeWaveParticipantView
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public string NamespaceName { get; set; } = null!;

    /// <summary>
    /// The Apply job whose probe step ran, so its logs can be read. Null until something probes,
    /// and overwritten if a probe is repeated.
    /// </summary>
    public Guid? LastProbeModuleJobId { get; set; }

    public List<SignalContributionView> Contributions { get; set; } = [];

    /// <summary>What this Module predicted, once it has probed.</summary>
    public List<SignalOutputView> Signals { get; set; } = [];

    public bool HasProbed { get; set; }

    /// <summary>Still waiting while any upstream might yet signal something it reads.</summary>
    public bool IsWaiting => Contributions.Any(c => c.Status == SignalContributionStatus.Potential);
}

public class SignalContributionView
{
    public Guid UpstreamParticipantId { get; set; }
    public Guid UpstreamModuleId { get; set; }
    public string UpstreamModuleName { get; set; } = null!;
    public SignalContributionStatus Status { get; set; }
}

/// <summary>
/// One predicted output change. The value is deliberately not decoded here: an unknown has no JSON
/// representation, so the view reports that the value is unknown rather than inventing one for it.
/// </summary>
public class SignalOutputView
{
    public string Name { get; set; } = null!;
    public bool Sensitive { get; set; }

    /// <summary>What the plan says happens to this output: create, update, delete, no-op or unknown.</summary>
    public string? Action { get; set; }

    /// <summary>True when the plan cannot know the new value yet, which is a finding rather than a gap.</summary>
    public bool AfterIsUnknown { get; set; }

    /// <summary>True when the output had no previous value, so there is nothing to compare against.</summary>
    public bool HasBefore { get; set; }
}
