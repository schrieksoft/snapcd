// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Contracts.Enums;

namespace SnapCd.Contracts.Dto.Transfers;

/// <summary>Opens a transfer from a source Module into one receiver.</summary>
public class TransferCreateRequestDto
{
    /// <summary>The Module on the other side. Exactly one: moving between three is two transfers.</summary>
    public Guid CounterpartyModuleId { get; set; }
}

/// <summary>The counterparty's answer to a transfer's request for consent.</summary>
public class ConsentRequestDto
{
    /// <summary>Whether the transfer may write into this Module's state.</summary>
    public bool Granted { get; set; }

    /// <summary>Free text shown beside the decision.</summary>
    public string? Reason { get; set; }
}

/// <summary>Starts one attempt at a transfer: which Modules move, and the refs they run against.</summary>
public class TransferRunRequestDto
{
    /// <summary>Whether both Modules move or only the one the transfer was started from.</summary>
    public TransferScope Scope { get; set; } = TransferScope.Both;

    /// <summary>The ref the starting Module runs against. Defaults to its own.</summary>
    public string? ModuleRef { get; set; }

    /// <summary>The ref the counterparty runs against. Defaults to its own.</summary>
    public string? CounterpartyRef { get; set; }
}

/// <summary>Closes a transfer, optionally saying why its ledger was left unaccounted for.</summary>
public class TransferCloseRequestDto
{
    /// <summary>Required when addresses are still open, and shown against each of them.</summary>
    public string? Reason { get; set; }
}
