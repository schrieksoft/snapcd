// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Enums;

/// <summary>
/// Where a Transfer stands, derived from its participant rows and the jobs run under it rather
/// than set directly.
/// </summary>
public enum TransferStatus
{
    /// <summary>Created; the receiver has not consented.</summary>
    Open,

    /// <summary>All consented; not every participant has a green proof against current inputs.</summary>
    Proving,

    /// <summary>Verdict green; not every participant has locked and declared merged.</summary>
    Merging,

    /// <summary>Every participant has declared merged; no Migrate running.</summary>
    ReadyToMigrate,

    Migrating,

    /// <summary>The source's verify succeeded; every hold released; closed.</summary>
    Migrated,

    /// <summary>Closed by the initiator; holds converted to pauses.</summary>
    Abandoned
}

/// <summary>Which side of the move a participant is on.</summary>
public enum TransferRole
{
    Source,
    Receiver
}

/// <summary>
/// Whether a participant has authorised the Transfer to prove and push against its Module.
/// Keyed to the map hash it was given against: editing the map returns it to Pending.
/// </summary>
public enum ConsentStatus
{
    /// <summary>The source, which consents by initiating.</summary>
    NotRequired,

    Pending,
    Granted,
    Refused,

    /// <summary>Withdrawn after being granted.</summary>
    Revoked
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
