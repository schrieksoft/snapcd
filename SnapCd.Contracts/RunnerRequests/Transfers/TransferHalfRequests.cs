// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests.Transfers;

/// <summary>What every half of a transfer needs, beyond the fields an ordinary job step carries.</summary>
public abstract class TransferHalfRequestBase : EngineJobRequestBase
{
    /// <summary>Which Module this half is for.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>This Module's root within its own checkout, passed as --root-dir.</summary>
    public string? RootDirectory { get; set; }

}

/// <summary>Pulls and pins this Module's state, ready to prove against.</summary>
/// <summary>Reads the committed map to learn which part this root plays. Runs no engine.</summary>
public class AnalyseTransferRefactorMapRequestBase : TransferHalfRequestBase
{
}

public class TransferMigrateMapRequestBase : TransferHalfRequestBase
{
    /// <summary>
    /// The source's fragment, for a receiver. demonolith runs one root at a time and never sees the
    /// other half's working directory, so the files are written into this root's before it runs.
    /// Null for the source, which produces them rather than consuming them.
    /// </summary>
    public string? SourceFragment { get; set; }

    /// <summary>The fragment's meta, which demonolith checks against the map before applying it.</summary>
    public string? SourceFragmentMeta { get; set; }
}

/// <summary>
/// Asks whether this Module's root plans to zero changes with the moved resources in place.
/// </summary>
public class TransferMigrateProveRequestBase : TransferHalfRequestBase
{
    /// <summary>
    /// The output values this root's plan reads from the other Module, written into the working
    /// directory before the prove runs. Null when the transfer crosses no seam this way.
    /// </summary>
    public string? ReceiverOutputs { get; set; }
}


/// <summary>
/// Writes this Module's share of the move into its own state. The receiver injects; the source
/// strips, and demonolith refuses to strip until the receiver's committed run receipt is in this
/// root's checkout.
/// </summary>
public class TransferMigrateRunRequestBase : TransferHalfRequestBase
{

}

/// <summary>Checks the written state plans clean, which is what closes this Module's move.</summary>
public class TransferMigrateVerifyRequestBase : TransferHalfRequestBase;
