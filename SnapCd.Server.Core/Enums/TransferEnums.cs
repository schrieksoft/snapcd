// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Enums;

/// <summary>Where a Transfer stands, derived from its runs' jobs rather than set directly.</summary>
public enum TransferStatus
{
    /// <summary>Created; the counterparty has not consented.</summary>
    Open,

    /// <summary>A job is running on one of its Modules.</summary>
    Migrating,

    /// <summary>Every Module in the run landed.</summary>
    Migrated,

    /// <summary>One Module landed and the other did not. Finishing it is a run of its own.</summary>
    PartiallyCompleted,

    /// <summary>No Module landed.</summary>
    Failed
}

/// <summary>
/// Whether the counterparty has authorised the Transfer to write into its Module's state. Answered
/// once: it holds until a run starts and means nothing after.
/// </summary>
public enum ConsentStatus
{
    /// <summary>The Module the transfer was started from, which consents by starting it.</summary>
    NotRequired,

    Pending,
    Granted,
    Refused
}

/// <summary>The outcome of one dispatched step.</summary>
public enum ManualJobStepStatus
{
    /// <summary>Not yet dispatched.</summary>
    Pending,

    /// <summary>Dispatched, no reply yet.</summary>
    Running,

    /// <summary>Exit 0.</summary>
    Succeeded,

    /// <summary>The slice ran and answered no (exit 2). Not a fault.</summary>
    Refused,

    /// <summary>The slice or its transport failed.</summary>
    Faulted,

    /// <summary>A producer this step depends on did not succeed.</summary>
    Skipped,

    /// <summary>A producer was retried after this step succeeded, so this result no longer counts.</summary>
    Stale
}
