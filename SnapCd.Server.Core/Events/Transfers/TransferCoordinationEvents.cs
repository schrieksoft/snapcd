// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.Events.Transfers;

// What the coordinator says to a participant, and what it hears back. The participant owns its own
// sequence and retries; the coordinator only ever says "start this" and is told how it went.

/// <summary>
/// Asks for a prove round, without knowing whether this Transfer has ever run one. The consumer
/// opens the coordinator on the first round and starts a round on every one after, so a caller
/// never has to know which.
/// </summary>
public class TransferProveRoundStartRequested
{
    public Guid TransferId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid JobId { get; set; }

    public string Map { get; set; } = null!;
    public string? MapHash { get; set; }

    public Guid SourceModuleId { get; set; }
    public Guid ReceiverModuleId { get; set; }

    public ResolvedModule SourceDeclared { get; set; } = null!;
    public ResolvedModule ReceiverDeclared { get; set; } = null!;

    public string? SourceProveRef { get; set; }
    public string? ReceiverProveRef { get; set; }

    public string? SourceRootDirectory { get; set; }
    public string? ReceiverRootDirectory { get; set; }
}

/// <summary>Starts the coordinator, once, when a Transfer is opened.</summary>
public class TransferOpened
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }

    public Guid SourceModuleId { get; set; }
    public Guid ReceiverModuleId { get; set; }

    /// <summary>The map's hash, which is the Transfer's identity.</summary>
    public string? MapHash { get; set; }

    public ResolvedModule SourceDeclared { get; set; } = null!;
    public ResolvedModule ReceiverDeclared { get; set; } = null!;

    public string? SourceRootDirectory { get; set; }
    public string? ReceiverRootDirectory { get; set; }
}

/// <summary>
/// Asks for a prove round: both Modules check out their own ref and plan, then the state fragment
/// and any values they need from each other cross between them.
/// </summary>
public class TransferProveRoundRequested
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The job the round runs under, so its steps and logs have somewhere to hang.</summary>
    public Guid JobId { get; set; }

    /// <summary>The map as it stands, passed to each slice.</summary>
    public string Map { get; set; } = null!;

    public string? SourceProveRef { get; set; }
    public string? ReceiverProveRef { get; set; }
}

/// <summary>Creates a participant's saga, once, when the Transfer opens.</summary>
public class TransferParticipantRegistered
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public TransferRole Role { get; set; }

    /// <summary>The Module's resolved configuration, which names the runner to dispatch to.</summary>
    public ResolvedModule Declared { get; set; } = null!;

    /// <summary>This participant's root within its own checkout.</summary>
    public string? RootDirectory { get; set; }
}

/// <summary>
/// Asks a Module to run its whole part of a round: check out its ref, plan, pull and pin its
/// state, and prove. It decides its own order and reports once, at the end.
///
/// Everything it needs from the other Module arrives here, because this is the only moment the
/// coordinator speaks to it during a round.
/// </summary>
public class TransferParticipantRunRequested
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The job the round runs under, so its steps and logs have somewhere to hang.</summary>
    public Guid JobId { get; set; }

    public int ProveRound { get; set; }

    /// <summary>The ref this Module consented to prove, overriding its own.</summary>
    public string? ProveRef { get; set; }

    /// <summary>The transfer map, written to the root before each slice.</summary>
    public string Map { get; set; } = null!;

    /// <summary>
    /// The source's fragment, for the receiver. Null for the source, which produces it.
    /// </summary>
    public string? FragmentState { get; set; }

    public string? FragmentMeta { get; set; }

    /// <summary>
    /// The values the other Module produced that this one needs, by filename. Empty when it needs
    /// nothing, which is why it can then run at the same time as the other.
    /// </summary>
    public Dictionary<string, string> Outputs { get; set; } = new();

    /// <summary>
    /// Stop after the state is pinned and the fragment written, without proving. Used for the
    /// source when the receiver has to prove first, because the receiver needs that fragment.
    /// </summary>
    public bool StopAfterMap { get; set; }
}

/// <summary>
/// A Module finished its part of a round. This is the only thing the coordinator hears from it, so
/// it carries everything the other Module might need.
/// </summary>
public class TransferParticipantRan
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public Guid ModuleId { get; set; }
    public TransferRole Role { get; set; }
    public int ProveRound { get; set; }

    /// <summary>What its prove ref resolved to on the runner.</summary>
    public string? DefinitiveRevision { get; set; }

    /// <summary>The fragment the source wrote, for the receiver.</summary>
    public string? FragmentState { get; set; }

    public string? FragmentMeta { get; set; }

    /// <summary>Module names this one needs values from, read out of the map by the runner.</summary>
    public List<string> NeedsValuesFrom { get; set; } = [];

    /// <summary>The values it produced that the map says the other Module needs.</summary>
    public Dictionary<string, string> Outputs { get; set; } = new();

    /// <summary>True when it only pinned its state, because it was told to stop after that.</summary>
    public bool StoppedAfterMap { get; set; }

    /// <summary>Zero when it proved clean, 2 when it did not, null when it did not prove.</summary>
    public int? ProveExitCode { get; set; }

    public string? Verdict { get; set; }
}

/// <summary>
/// Tells a participant to stop where it is. Cancellation is a whole-Transfer act - there is no
/// releasing one participant mid-transfer - so it arrives here from the coordinator rather than
/// from an operator.
/// </summary>
public class TransferParticipantStopRequested
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>Kill the step in flight, or let it finish and stop after it.</summary>
    public bool Immediate { get; set; }
}

/// <summary>
/// A participant's sequence stopped. Whether that ends the round is the coordinator's call: a
/// refusal is a red verdict, a fault may be retried.
/// </summary>
public class TransferParticipantStopped
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public Guid ModuleId { get; set; }
    public TransferRole Role { get; set; }
    public int ProveRound { get; set; }

    /// <summary>Which task it stopped on.</summary>
    public string Task { get; set; } = null!;

    /// <summary>Refused when the slice answered no, Faulted when it or its transport broke.</summary>
    public ManualJobStepStatus Status { get; set; }

    public string? ErrorHeader { get; set; }
    public string? Error { get; set; }
}
