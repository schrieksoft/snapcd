// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.OutputSets;

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
}

/// <summary>The map slice's reply: this root's state is pinned and ready to prove against.</summary>
public class TransferMigrateMapCompleted : TransferStepResponseBase
{
    /// <summary>
    /// Output names this root's plan consumes from the other Module, read from the map. Empty
    /// unless the transfer moves something this root still refers to.
    /// </summary>
    public List<string> NeedsOutputs { get; set; } = [];
}

public class TransferMigrateMapFaulted : TransferStepFaultedBase;

/// <summary>
/// `transfer migrate prove` for one participant: does this root plan to zero changes with the
/// moved resources in place and the producer values it consumes supplied.
/// </summary>
public class TransferMigrateProveRequested : TransferStepRequestBase
{

}

/// <summary>
/// The proof for one participant. Exit 2 is a refusal, not a fault: the slice ran and answered no.
/// </summary>
public class TransferMigrateProveCompleted : TransferStepResponseBase
{
    /// <summary>0 when the root planned clean, 2 when it did not.</summary>
    public int ExitCode { get; set; }

    /// <summary>The outputs this root's plan produced, by filename.</summary>
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
    /// <summary>The addresses this Module's state gave up or took on.</summary>
    public List<string> TransferredAddresses { get; set; } = [];
}

public class TransferMigrateRunFaulted : TransferStepFaultedBase;

public class TransferMigrateVerifyRequested : TransferStepRequestBase
{
}

public class TransferMigrateVerifyCompleted : TransferStepResponseBase;

public class TransferMigrateVerifyFaulted : TransferStepFaultedBase;

/// <summary>
/// Reads this Module's outputs after its state has been written, so the other side of the transfer
/// can plan against values that now exist.
/// </summary>
public class TransferOutputsRequested : TransferStepRequestBase
{
}

/// <summary>The outputs this Module's state now produces, stored against it.</summary>
public class TransferOutputsCompleted : TransferStepResponseBase
{
    public OutputSetCreateDto? OutputSet { get; set; }
}

public class TransferOutputsFaulted : TransferStepFaultedBase;
