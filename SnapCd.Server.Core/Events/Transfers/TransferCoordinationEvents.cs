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
