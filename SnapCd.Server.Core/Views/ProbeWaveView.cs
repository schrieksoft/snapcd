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

    /// <summary>
    /// Whether the approval this wave informs is still open. A closed wave takes no further asking:
    /// its predictions are a record of what was predicted, not a claim about the present.
    /// </summary>
    public bool IsLive { get; set; }

    /// <summary>
    /// What the wave says about this change as a whole, for a reader about to approve it.
    ///
    /// Failed and Impact are read before Pending, so one bad probe or one destructive prediction
    /// shows while the rest of the wave is still running. A wave cannot be called cleared until
    /// nothing is outstanding: how far a change ripples is not known when it starts, so every
    /// Module that owes a probe may still enrol Modules nobody has heard of yet.
    /// </summary>
    public DownstreamImpact Impact =>
        Modules.Any(m => m.Progress == ProbeProgress.Faulted) ? DownstreamImpact.Failed
        : Modules.Any(m => m.HasDisruptiveSignal) ? DownstreamImpact.Impact
        : Modules.Any(m => m.IsOutstanding) ? DownstreamImpact.Pending
        : DownstreamImpact.Cleared;
}

/// <summary>
/// Where a wave has got to, as the approval screen states it. A wave that does not exist has no
/// value here: nothing downstream reads what the plan changes, which is not the same answer as
/// a check that ran and found nothing.
/// </summary>
public enum DownstreamImpact
{
    /// <summary>Something is still to be checked, and what else it reaches is not yet known.</summary>
    Pending,

    /// <summary>A probe did not report, so part of the effect is unknown and waiting will not fix it.</summary>
    Failed,

    /// <summary>Everything answered and something downstream is destroyed or replaced.</summary>
    Impact,

    /// <summary>Everything answered and nothing downstream is disrupted.</summary>
    Cleared
}

public class ProbeWaveModuleView
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public string NamespaceName { get; set; } = null!;
    public string StackName { get; set; } = null!;

    /// <summary>
    /// Every probe this Module has run for the wave, newest first. A probe is repeatable, so a
    /// Module owing a wave across several Applies has one attempt per run.
    /// </summary>
    public List<ProbeAttemptView> Attempts { get; set; } = [];

    /// <summary>
    /// The attempt the approval decision turns on, which is the most recent one.
    /// </summary>
    public ProbeAttemptView? Latest => Attempts.Count == 0 ? null : Attempts[0];

    /// <summary>
    /// The Apply job carrying the probe, so its logs can be read. Null until one is started.
    /// </summary>
    public Guid? ProbeModuleJobId => Latest?.ProbeModuleJobId;

    public List<SignalEdgeView> Edges { get; set; } = [];

    /// <summary>What this Module predicted, once it has probed.</summary>
    public List<SignalOutputView> Signals => Latest?.Signals ?? [];

    public bool HasProbed => Latest?.HasProbed == true;

    /// <summary>
    /// How the probe job ended, where one was started. The attempt row keeps no status of its own,
    /// because the job carrying the probe already has one.
    /// </summary>
    public ExecutionStatus? ProbeJobStatus => Latest?.ProbeJobStatus;

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

    /// <summary>
    /// A probe that ended without reporting. Its answer is not coming, so a wave holding one is
    /// never going to settle on its own.
    /// </summary>
    public bool HasFaulted =>
        !HasProbed &&
        ProbeJobStatus is ExecutionStatus.Failed
            or ExecutionStatus.Orphaned
            or ExecutionStatus.PolicyDenied
            or ExecutionStatus.Cancelled
            or ExecutionStatus.NotApproved;

    /// <summary>
    /// This Module is why the wave is not finished: an upstream still owes it an answer, or it
    /// owes one itself. Mirrors ModuleProgress.IsOutstanding, which the readiness check reads.
    /// </summary>
    public bool IsOutstanding =>
        !HasProbed && !HasFaulted && (IsWaiting || !NothingToProbe);

    public ProbeProgress Progress =>
        HasProbed ? ProbeProgress.Reported
        : HasFaulted ? ProbeProgress.Faulted
        : IsWaiting ? ProbeProgress.WaitingOnUpstream
        : NothingToProbe ? ProbeProgress.NothingToProbe
        : ProbeModuleJobId != null ? ProbeProgress.Running
        : ProbeProgress.Queued;

    /// <summary>
    /// A predicted change that destroys or replaces something is the finding worth interrupting
    /// someone for, as opposed to one that only creates or updates.
    /// </summary>
    public bool HasDisruptiveSignal =>
        Signals.Any(s => s.Actions.Contains("delete"));

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
    NothingToProbe,

    /// <summary>Its probe ended without reporting, so its prediction will not arrive.</summary>
    Faulted
}

/// <summary>One probe a Module ran for a wave, with what it predicted and where its logs are.</summary>
/// <summary>What moved under a probe after it answered.</summary>
public enum StalenessReason
{
    Revision,
    Declaration,
    Parameters,
    EnvironmentVariables
}

public class ProbeAttemptView
{
    /// <summary>The Apply job that carried this probe, which is where its logs live.</summary>
    public Guid? ProbeModuleJobId { get; set; }

    /// <summary>When the probe was recorded, as the attempt row stores it.</summary>
    public long Timestamp { get; set; }

    /// <summary>The probe job's number, which is how a reader refers to it.</summary>
    public int? ProbeJobNumber { get; set; }

    /// <summary>True once the probe reported a prediction, as opposed to ending without one.</summary>
    public bool HasProbed { get; set; }

    /// <summary>How the probe job ended, where one was started.</summary>
    public ExecutionStatus? ProbeJobStatus { get; set; }

    /// <summary>What this probe predicted.</summary>
    public List<SignalOutputView> Signals { get; set; } = [];

    /// <summary>
    /// What has moved for this Module since the probe answered. A probe plans against the code,
    /// the declaration and the resolved input values of one moment, so a prediction holds only
    /// while those are unchanged. Empty where nothing has moved, and null where the comparison
    /// cannot be made: an attempt that never reached a probe step records none of them.
    /// </summary>
    public List<StalenessReason>? Stale { get; set; }

    public bool IsStale => Stale is { Count: > 0 };
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

    /// <summary>
    /// The plan's own actions for this output, as "tofu show -json" writes them: one element for a
    /// simple action, two for a replace.
    /// </summary>
    public List<string> Actions { get; set; } = [];

    /// <summary>The actions as one phrase, for display: "update", or "delete then create".</summary>
    public string? Action => Actions.Count == 0 ? null : string.Join(" then ", Actions);

    /// <summary>True where the output goes away entirely, rather than being replaced.</summary>
    public bool IsDelete => Actions is ["delete"];

    /// <summary>True when the plan cannot know the new value yet, which is a finding rather than a gap.</summary>
    public bool AfterIsUnknown { get; set; }

    /// <summary>True when the output had no previous value, so there is nothing to compare against.</summary>
    public bool HasBefore { get; set; }
}
