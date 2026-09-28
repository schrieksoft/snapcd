// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// Asks a manual job that has just left a wait to dispatch the step it was waiting to run. The
/// answer to the wait and the dispatch are two consumes rather than one, so the saga is already in
/// the state that expects the reply before the step is asked for. Publishing both from one chain
/// lets a fast answer arrive while the saga is still in the state it is leaving.
///
/// Each job kind names its own, as it names its own steps: a message type is what says which saga
/// a message is for, and two sagas sharing one means both are subscribed to every copy.
/// </summary>
public abstract class ManualJobResumeEventBase
{
    /// <summary>The job that was waiting.</summary>
    public Guid ModuleJobId { get; set; }

    public Guid OrganizationId { get; set; }
}

/// <summary>The counterparty agreed, so this side may start.</summary>
public class TransferConsented : ManualJobResumeEventBase;

/// <summary>A transfer's approval is answered, so the write it was holding may go ahead.</summary>
public class TransferApproved : ManualJobResumeEventBase;

/// <summary>
/// The values a transfer's Module was waiting on exist, so it can prove. Separate from the approval
/// because it resumes a different step: one event per thing that was being waited for.
/// </summary>
public class TransferOutputsArrived : ManualJobResumeEventBase;

/// <summary>
/// A state edit's threshold is answered. One gate, so the fact is the approval itself rather than
/// the job carrying on: a job that arrives already approved never waits, and is still approved.
/// </summary>
public class MoveApproved : ManualJobResumeEventBase;

public class ImportApproved : ManualJobResumeEventBase;

public class RemoveApproved : ManualJobResumeEventBase;
