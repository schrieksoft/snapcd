// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

/// <summary>
/// The last three steps an apply and a destroy both run: resolve the ref, check the policies,
/// read the outputs. The runner does the same work for either, but each job kind has its own
/// messages and its own endpoints, as every other step does.
/// </summary>
public abstract class GetDefinitiveRevisionRequestedBase : StepRequestBase;

public abstract class GetDefinitiveRevisionCompletedBase : StepResponseBase
{
    public string DefinitiveRevision { get; set; } = string.Empty;
}

public abstract class PolicyValidateRequestedBase : StepRequestBase;

public abstract class PolicyValidateCompletedBase : StepResponseBase
{
    public PolicyOutcome Outcome { get; set; }
}

public abstract class OutputRequestedBase : StepRequestBase;

public class ApplyGetDefinitiveRevisionRequested : GetDefinitiveRevisionRequestedBase;
public class ApplyGetDefinitiveRevisionCompleted : GetDefinitiveRevisionCompletedBase;
public class ApplyGetDefinitiveRevisionCancelled : StepResponseBase;
public class ApplyGetDefinitiveRevisionFaulted : StepFaultedBase;

public class ApplyPolicyValidateRequested : PolicyValidateRequestedBase;
public class ApplyPolicyValidateCompleted : PolicyValidateCompletedBase;
public class ApplyPolicyValidateCancelled : StepResponseBase;
public class ApplyPolicyValidateFaulted : StepFaultedBase;

public class ApplyOutputRequested : OutputRequestedBase;
public class ApplyOutputCompleted : StepResponseBase;
public class ApplyOutputCancelled : StepResponseBase;
public class ApplyOutputFaulted : StepFaultedBase;

public class DestroyGetDefinitiveRevisionRequested : GetDefinitiveRevisionRequestedBase;
public class DestroyGetDefinitiveRevisionCompleted : GetDefinitiveRevisionCompletedBase;
public class DestroyGetDefinitiveRevisionCancelled : StepResponseBase;
public class DestroyGetDefinitiveRevisionFaulted : StepFaultedBase;

public class DestroyPolicyValidateRequested : PolicyValidateRequestedBase;
public class DestroyPolicyValidateCompleted : PolicyValidateCompletedBase;
public class DestroyPolicyValidateCancelled : StepResponseBase;
public class DestroyPolicyValidateFaulted : StepFaultedBase;

public class DestroyOutputRequested : OutputRequestedBase;
public class DestroyOutputCompleted : StepResponseBase;
public class DestroyOutputCancelled : StepResponseBase;
public class DestroyOutputFaulted : StepFaultedBase;
