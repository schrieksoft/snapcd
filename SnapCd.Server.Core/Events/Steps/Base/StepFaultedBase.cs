// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Events.Steps.Base;

/// <summary>
/// Base class for all step faulted events.
/// Contains common error information and the IsServerSideError flag to distinguish
/// between server-side errors (in consumers/activities) and runner-side errors.
/// </summary>
public abstract class StepFaultedBase : StepResponseBase
{
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }

    /// <summary>
    /// When true, indicates the error occurred on the server side (in consumers or activities).
    /// When false (default), indicates the error occurred on the runner side.
    /// </summary>
    public bool IsServerSideError { get; set; }

    /// <summary>
    /// Set when the fault was caused by a policy denial rather than breakage — e.g. a mandatory
    /// CrossGuard violation failing the Pulumi preview. Routes the job to PolicyDenied instead of Failed.
    /// </summary>
    public PolicyOutcome? PolicyOutcome { get; set; }
}
