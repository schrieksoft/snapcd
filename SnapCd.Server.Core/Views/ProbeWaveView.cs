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

    public DateTime CreatedDateTime { get; set; }

    public List<ProbeWaveModuleView> Modules { get; set; } = [];

    /// <summary>
    /// Modules left out because the reader cannot see them. Counted rather than hidden: a wave
    /// that silently looks complete is worse than one that says what is missing.
    /// </summary>
    public int ModulesOmitted { get; set; }

    /// <summary>A wave is still going while any participant is still waiting on something.</summary>
    public bool IsSettled => Modules.All(m => !m.IsWaiting);
}

public class ProbeWaveModuleView
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public string NamespaceName { get; set; } = null!;

    /// <summary>
    /// The Apply job carrying the probe, so its logs can be read. Null until one is started.
    /// </summary>
    public Guid? ProbeModuleJobId { get; set; }

    public List<SignalEdgeView> Edges { get; set; } = [];

    /// <summary>What this Module predicted, once it has probed.</summary>
    public List<SignalOutputView> Signals { get; set; } = [];

    public bool HasProbed { get; set; }

    /// <summary>Still waiting while any upstream might yet signal something it reads.</summary>
    public bool IsWaiting => Edges.Any(e => e.Status == SignalEdgeStatus.Potential);

    /// <summary>
    /// Nothing reaches this Module along the edges that resolved, so it has no probe to run. Edges
    /// are written from reachability, so most of a wide graph is in this state.
    /// </summary>
    public bool NothingToProbe =>
        !IsWaiting &&
        !HasProbed &&
        !Edges.Any(e => e.Status == SignalEdgeStatus.Confirmed);

    public ProbeProgress Progress =>
        HasProbed ? ProbeProgress.Reported
        : IsWaiting ? ProbeProgress.WaitingOnUpstream
        : NothingToProbe ? ProbeProgress.NothingToProbe
        : ProbeModuleJobId != null ? ProbeProgress.Running
        : ProbeProgress.Queued;

    /// <summary>
    /// A predicted change that destroys or replaces something is the finding worth interrupting
    /// someone for, as opposed to one that only creates or updates.
    /// </summary>
    public bool HasDisruptiveSignal =>
        Signals.Any(s => s.Action is "delete" or "replace");

    public bool HasUnknownSignal => Signals.Any(s => s.AfterIsUnknown);
}

/// <summary>How far one participant has got, for a reader deciding whether to wait for it.</summary>
public enum ProbeProgress
{
    /// <summary>An upstream has not answered, so it is not yet known whether this Module probes.</summary>
    WaitingOnUpstream,

    /// <summary>Released and waiting for its Apply to start.</summary>
    Queued,

    /// <summary>Its Apply is running and the probe has not reported.</summary>
    Running,

    /// <summary>It probed and its prediction is recorded.</summary>
    Reported,

    /// <summary>Resolved with nothing reaching it, so no probe runs.</summary>
    NothingToProbe
}

public class SignalEdgeView
{
    public Guid UpstreamModuleId { get; set; }
    public string UpstreamModuleName { get; set; } = null!;
    public SignalEdgeStatus Status { get; set; }
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
