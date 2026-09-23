// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.Steps.Transfer;

// The demonolith slices. Each anchors on the one root it is run against, with only that root's
// checkout and credentials; the files they exchange travel through the server as job artefacts
// rather than between runners directly.

/// <summary>
/// `transfer migrate map` for one participant: pulls and pins that root's state. On the source it
/// also writes the receiver's fragment; on the receiver it applies the fragment it is given.
/// </summary>
public class TransferMigrateMapRequested : TransferStepRequestBase
{

    /// <summary>
    /// The source's fragment, for the receiver's slice. Null on the source, which produces it.
    /// </summary>
    public string? FragmentState { get; set; }

    /// <summary>The fragment's metadata, which pins it to a map hash and a source serial.</summary>
    public string? FragmentMeta { get; set; }
}

/// <summary>
/// The map slice's reply. The source's carries the fragment it wrote, which the server stores and
/// hands to the receiver.
/// </summary>
public class TransferMigrateMapCompleted : TransferStepResponseBase
{
    public string? FragmentState { get; set; }
    public string? FragmentMeta { get; set; }

    /// <summary>The map hash the slice worked against, checked against what the saga expects.</summary>
    public string? MapHash { get; set; }

    /// <summary>
    /// Module names this one needs values from, read by the runner out of the map. When a Module
    /// needs a value the other one produces, that other one has to plan first so the value exists,
    /// which is what decides the order the two proofs run in.
    /// </summary>
    public List<string> NeedsValuesFrom { get; set; } = [];
}

public class TransferMigrateMapFaulted : TransferStepFaultedBase;

/// <summary>
/// `transfer migrate prove` for one participant: does this root plan to zero changes with the
/// moved resources in place and the producer values it consumes supplied.
/// </summary>
public class TransferMigrateProveRequested : TransferStepRequestBase
{

    /// <summary>
    /// The outputs artefacts this participant consumes, by filename, as the runner writes them into
    /// the slice's work directory. Empty when nothing is threaded into this participant.
    /// </summary>
    public Dictionary<string, string> Outputs { get; set; } = new();
}

/// <summary>
/// The proof for one participant. Exit 2 is a refusal, not a fault: the slice ran and answered no.
/// </summary>
public class TransferMigrateProveCompleted : TransferStepResponseBase
{
    /// <summary>0 when the root planned clean, 2 when it did not.</summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// The outputs this participant produced that the map says another consumes, by filename. The
    /// server stores them for the consumer's slice.
    /// </summary>
    public Dictionary<string, string> Outputs { get; set; } = new();

    /// <summary>Why it refused, when it did.</summary>
    public string? Verdict { get; set; }
}

public class TransferMigrateProveFaulted : TransferStepFaultedBase;

/// <summary>
/// `transfer refactor diff` for one participant: does the code in this root still match its own
/// copy of the map. Checks that root alone, which is the command's default.
/// </summary>
public class TransferRefactorDiffRequested : TransferStepRequestBase
{
}

public class TransferRefactorDiffCompleted : TransferStepResponseBase
{
    /// <summary>0 when in sync, 2 when the committed code has drifted from the map.</summary>
    public int ExitCode { get; set; }

    public string? Verdict { get; set; }
}

public class TransferRefactorDiffFaulted : TransferStepFaultedBase;

public class TransferMigrateRunRequested : TransferStepRequestBase
{

}

public class TransferMigrateRunCompleted : TransferStepResponseBase
{
}

public class TransferMigrateRunFaulted : TransferStepFaultedBase;

public class TransferMigrateVerifyRequested : TransferStepRequestBase
{
}

public class TransferMigrateVerifyCompleted : TransferStepResponseBase;

public class TransferMigrateVerifyFaulted : TransferStepFaultedBase;
