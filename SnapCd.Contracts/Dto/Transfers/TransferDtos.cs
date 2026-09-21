// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Contracts.Dto.Transfers;

/// <summary>Opens a transfer from a source Module into one receiver.</summary>
public class TransferCreateRequestDto
{
    /// <summary>The Module the resources move into. Exactly one: moving into two is two transfers.</summary>
    public Guid ReceiverModuleId { get; set; }

    /// <summary>The transfer map, as demonolith wrote it. Hashed verbatim, never re-serialised.</summary>
    public string Map { get; set; } = null!;

    /// <summary>The ref to prove, defaulting the receiver's until it says otherwise.</summary>
    public string? ProveRef { get; set; }
}

/// <summary>A participant's answer to a transfer's request for consent.</summary>
public class ConsentRequestDto
{
    /// <summary>Whether the transfer may prove and push into this Module.</summary>
    public bool Granted { get; set; }

    /// <summary>The ref this Module wants proved.</summary>
    public string? ProveRef { get; set; }

    /// <summary>Free text shown beside the decision.</summary>
    public string? Reason { get; set; }
}

/// <summary>Changes the ref a Module wants proved.</summary>
public class ProveRefRequestDto
{
    /// <summary>The branch, tag or commit to prove against.</summary>
    public string Ref { get; set; } = null!;
}

/// <summary>Free text explaining an action, shown wherever the action is reported.</summary>
public class ReasonRequestDto
{
    /// <summary>Why the action was taken.</summary>
    public string? Reason { get; set; }
}

/// <summary>Declares that a participant's proved code is on its configured branch.</summary>
public class DeclareMergedRequestDto
{
    /// <summary>The commit the proved code landed as; Migrate checks its ancestry before pushing.</summary>
    public string MergedCommit { get; set; } = null!;
}

/// <summary>Replaces a transfer's map, which re-asks the receiver for consent.</summary>
public class ReplaceMapRequestDto
{
    /// <summary>The new transfer map, hashed verbatim to become the transfer's new identity.</summary>
    public string Map { get; set; } = null!;
}
