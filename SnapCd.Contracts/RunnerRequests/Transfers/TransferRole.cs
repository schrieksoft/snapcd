// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests.Transfers;

public enum TransferRoleKind
{
    /// <summary>The map could not be read, or does not describe this root. The job stops.</summary>
    Unknown,

    /// <summary>Gives the resources away, and cuts the fragment the receiver needs.</summary>
    Source,

    /// <summary>Takes the resources in, and cannot start until the source's fragment is here.</summary>
    Receiver
}

/// <summary>
/// Which part of a transfer a root plays, read from the committed map. The receiver's map step
/// fails outright without the source's fragment, so this is read before that step rather than
/// discovered by attempting it.
/// </summary>
public class TransferRole
{
    public TransferRoleKind Kind { get; set; }

    /// <summary>Why the role could not be determined, for the step that reports the failure.</summary>
    public string? Problem { get; set; }
}
