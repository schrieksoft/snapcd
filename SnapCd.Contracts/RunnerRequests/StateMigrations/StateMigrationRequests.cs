// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Contracts.RunnerRequests.StateMigrations;

/// <summary>
/// Asks which of the given addresses are in this Module's state. The filter is the whole request:
/// a Module's full state is never listed to the logs or returned, only the verdict on these.
/// </summary>
public class StateListFilteredRequestBase : EngineJobRequestBase
{
    public List<string> Addresses { get; set; } = [];
}

/// <summary>One address, as the runner found or left it.</summary>
public class StateAddressResult
{
    public string Address { get; set; } = null!;

    /// <summary>An mv's destination or an import's resource id, where the operation has one.</summary>
    public string? Target { get; set; }

    /// <summary>"Present" or "Absent" from a list; "Succeeded" or "Failed" from a move.</summary>
    public string Outcome { get; set; } = null!;
}

/// <summary>
/// One address to move, and where to. A batch runs its addresses independently: recording which of
/// them worked is the point, so one failure never abandons the rest.
/// </summary>
public class StateAddressInstruction
{
    public string Address { get; set; } = null!;

    /// <summary>An mv's destination address, or an import's resource id. Null for a remove.</summary>
    public string? Target { get; set; }
}

/// <summary>
/// A batch of addresses to move, import or remove. The three share a request because they differ
/// only in the command each address is run through.
/// </summary>
public class StateMoveRequestBase : EngineJobRequestBase
{
    /// <summary>"Mv", "Import" or "Remove".</summary>
    public string Operation { get; set; } = null!;

    public List<StateAddressInstruction> Instructions { get; set; } = [];
}
