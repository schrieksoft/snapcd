// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.RunnerRequests.Transfers;

namespace SnapCd.Server.Core.Events.Steps.Transfer;

// The demonolith steps. Each anchors on the one root it is run against, with only that root's
// checkout and credentials; the files they exchange travel through the server as job artefacts
// rather than between runners directly.

/// <summary>
/// `transfer migrate map` for one participant: pulls and pins that root's state. On the source it
/// also writes the receiver's fragment; on the receiver it applies the fragment it is given.
/// </summary>
public class TransferMigrateMapRequested : TransferStepRequestBase
{
    /// <summary>The source's fragment, for a receiver; null on the source, which produces it.</summary>
    public string? SourceFragment { get; set; }

    public string? SourceFragmentMeta { get; set; }
}

/// <summary>The map step's reply: this root's state is pinned and ready to prove against.</summary>
public class TransferMigrateMapCompleted : TransferStepResponseBase
{
    /// <summary>The fragment a source cut for the receiver; null when this half is the receiver.</summary>
    public string? SourceFragment { get; set; }

    public string? SourceFragmentMeta { get; set; }
}

public class AnalyseTransferRefactorMapRequested : TransferStepRequestBase;

/// <summary>
/// What the committed map says about this root, read before anything runs. A receiver's map step
/// fails outright without the source's fragment, so the role has to be known before that step
/// rather than discovered by attempting it.
/// </summary>
public class AnalyseTransferRefactorMapCompleted : TransferStepResponseBase
{
    public TransferRoleKind Role { get; set; }

    /// <summary>
    /// Output names this root's plan consumes from the other Module. Empty unless the transfer
    /// moves something this root still refers to.
    /// </summary>
    public List<string> NeedsOutputs { get; set; } = [];

    /// <summary>Why the map could not be read, when it could not.</summary>
    public string? Problem { get; set; }
}

public class AnalyseTransferRefactorMapCancelled : TransferStepCancelledBase;

public class AnalyseTransferRefactorMapFaulted : TransferStepFaultedBase;

public class TransferMigrateMapCancelled : TransferStepCancelledBase;

public class TransferMigrateMapFaulted : TransferStepFaultedBase;

/// <summary>
/// `transfer migrate prove` for one participant: does this root plan to zero changes with the
/// moved resources in place and the producer values it consumes supplied.
/// </summary>
public class TransferMigrateProveRequested : TransferStepRequestBase
{
    /// <summary>The output values this root's plan reads from the other Module.</summary>
    public string? ReceiverOutputs { get; set; }
}

/// <summary>
/// The proof for one participant. Exit 2 is a refusal, not a fault: the step ran and answered no.
/// </summary>
public class TransferMigrateProveCompleted : TransferStepResponseBase
{
    /// <summary>0 when the root planned clean, 2 when it did not.</summary>
    public int ExitCode { get; set; }

    /// <summary>The output values this root's plan produced, as demonolith wrote them.</summary>
    public string? Outputs { get; set; }

    /// <summary>Why it refused, when it did.</summary>
    public string? Verdict { get; set; }
}

public class TransferMigrateProveCancelled : TransferStepCancelledBase;

public class TransferMigrateProveFaulted : TransferStepFaultedBase;

public class TransferMigrateRunRequested : TransferStepRequestBase
{

}

public class TransferMigrateRunCompleted : TransferStepResponseBase
{
    /// <summary>The addresses this Module's state gave up or took on.</summary>
    public List<string> TransferredAddresses { get; set; } = [];

    /// <summary>
    /// Whether this Module gave the addresses up or took them on, as demonolith derived it from the
    /// map. Snap CD does not decide the roles; it is told them.
    /// </summary>
    public bool GaveUp { get; set; }
}

public class TransferMigrateRunCancelled : TransferStepCancelledBase;

public class TransferMigrateRunFaulted : TransferStepFaultedBase;

public class TransferMigrateVerifyRequested : TransferStepRequestBase
{
}

public class TransferMigrateVerifyCompleted : TransferStepResponseBase;

public class TransferMigrateVerifyCancelled : TransferStepCancelledBase;

public class TransferMigrateVerifyFaulted : TransferStepFaultedBase;




