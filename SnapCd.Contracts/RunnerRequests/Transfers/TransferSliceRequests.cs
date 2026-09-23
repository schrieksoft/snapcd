// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests.Transfers;

/// <summary>
/// What every transfer slice needs. The Module id is on the request because a transfer runs the
/// same slice against two Modules, and each reply has to say which one it came from.
/// </summary>
public abstract class TransferSliceRequestBase : EngineJobRequestBase
{
    /// <summary>Which Module this slice is for.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>This Module's root within its own checkout, passed as --root-dir.</summary>
    public string? RootDirectory { get; set; }

}

/// <summary>
/// Pulls and pins this Module's state. On the source it also writes the fragment the receiver
/// needs; on the receiver it applies the fragment it is given.
/// </summary>
public class TransferMigrateMapRequestBase : TransferSliceRequestBase
{
    /// <summary>The fragment to apply, for the receiver. Null on the source, which writes it.</summary>
    public string? FragmentState { get; set; }

    public string? FragmentMeta { get; set; }
}

/// <summary>
/// Asks whether this Module's root plans to zero changes with the moved resources in place, given
/// the values the other Module produced.
/// </summary>
public class TransferMigrateProveRequestBase : TransferSliceRequestBase
{
    /// <summary>The output files this Module consumes, by the filename demonolith expects.</summary>
    public Dictionary<string, string> Outputs { get; set; } = new();
}

/// <summary>
/// Asks whether the code in this root still matches its own copy of the map. Checks that root
/// alone, which is what the command does by default.
/// </summary>
public class TransferRefactorDiffRequestBase : TransferSliceRequestBase;

/// <summary>
/// Writes this Module's share of the move into its own state. The receiver injects; the source
/// strips, and demonolith refuses to strip until the receiver's committed run receipt is in this
/// root's checkout.
/// </summary>
public class TransferMigrateRunRequestBase : TransferSliceRequestBase
{

}

/// <summary>Checks the written state plans clean, which is what closes this Module's move.</summary>
public class TransferMigrateVerifyRequestBase : TransferSliceRequestBase;
