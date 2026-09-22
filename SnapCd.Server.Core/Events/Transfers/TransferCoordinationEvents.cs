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
/// Asks a participant to run its preamble - check out its ref, init, validate and plan - for the
/// round named. It reports back when every step of that sequence has landed.
/// </summary>
public class TransferParticipantPrepareRequested
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The job the sequence runs under, so its steps and logs have somewhere to hang.</summary>
    public Guid JobId { get; set; }

    public int ProveRound { get; set; }

    /// <summary>The ref this participant consented to prove, overriding the Module's own.</summary>
    public string? ProveRef { get; set; }
}

/// <summary>
/// A participant finished its preamble. Carries the resolved commit, which is what the coordinator
/// keys the proof against, and whether the plan was clean.
/// </summary>
public class TransferParticipantPrepared
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public Guid ModuleId { get; set; }
    public TransferRole Role { get; set; }
    public int ProveRound { get; set; }

    /// <summary>What the prove ref resolved to on the runner.</summary>
    public string? DefinitiveRevision { get; set; }

    /// <summary>Anything but zero is a red plan for this participant.</summary>
    public int TotalChangedCount { get; set; }
}

/// <summary>
/// Asks a participant to run its map slice: pull and pin its own state. The source writes the
/// fragment the receiver needs; the receiver is given that fragment and applies it to its copy.
/// </summary>
public class TransferParticipantMapRequested
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The transfer map, written to the root before the slice runs.</summary>
    public string Map { get; set; } = null!;

    /// <summary>The source's fragment, for the receiver. Null on the source, which produces it.</summary>
    public string? FragmentState { get; set; }

    public string? FragmentMeta { get; set; }
}

/// <summary>A participant's map slice landed. The source's carries the fragment it wrote.</summary>
public class TransferParticipantMapped
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public Guid ModuleId { get; set; }
    public TransferRole Role { get; set; }
    public int ProveRound { get; set; }

    public string? FragmentState { get; set; }
    public string? FragmentMeta { get; set; }
    public string? MapHash { get; set; }

    /// <summary>
    /// Module names this one needs values from. A Module that needs a value the other produces
    /// cannot plan until the other has, so this is what orders the two proofs.
    /// </summary>
    public List<string> NeedsValuesFrom { get; set; } = [];
}

/// <summary>
/// Asks a participant to prove: does its root plan to zero changes with the moved resources in
/// place. The coordinator supplies whatever producer values this one consumes, which is why the
/// order matters when the map has a cross edge.
/// </summary>
public class TransferParticipantProveRequested
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }

    public string Map { get; set; } = null!;

    /// <summary>The outputs artefacts this participant consumes, by filename.</summary>
    public Dictionary<string, string> Outputs { get; set; } = new();

    /// <summary>The fingerprint this proof will be recorded against.</summary>
    public string? InputKey { get; set; }
}

/// <summary>
/// A participant's proof. Exit 2 is a refusal rather than a fault, and the outputs are whatever the
/// map says another participant consumes.
/// </summary>
public class TransferParticipantProved
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public Guid ModuleId { get; set; }
    public TransferRole Role { get; set; }
    public int ProveRound { get; set; }

    /// <summary>0 when the root planned clean, 2 when it did not.</summary>
    public int ExitCode { get; set; }

    public Dictionary<string, string> Outputs { get; set; } = new();

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
