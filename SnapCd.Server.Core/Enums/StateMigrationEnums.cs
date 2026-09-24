// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Enums;

/// <summary>What a job did to one address. One table serves every operation, so this says which.</summary>
public enum AddressOperation
{
    /// <summary>Moved within a state, or into another Module's.</summary>
    Mv,

    /// <summary>Brought existing infrastructure under management.</summary>
    Import,

    /// <summary>Dropped from state, leaving the infrastructure.</summary>
    Remove,

    /// <summary>Asked whether the address is in a state, changing nothing.</summary>
    List,

    /// <summary>Carried across a transfer.</summary>
    Transfer
}

/// <summary>
/// What the job did to this address on this Module: gave it up, took it on, or only asked. It is
/// what lets one table serve every operation.
/// </summary>
public enum AddressDirection
{
    Left,
    Arrived,
    Checked
}

/// <summary>
/// How one address fared. A move reports whether it worked; a list reports what it saw, which is a
/// different fact - "the command said it worked" and "the address is there" are separate.
/// </summary>
public enum AddressOutcome
{
    Succeeded,
    Failed,
    Present,
    Absent
}
