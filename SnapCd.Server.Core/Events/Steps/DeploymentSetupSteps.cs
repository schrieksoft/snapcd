// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

/// <summary>
/// The steps an apply and a destroy both run before their own work: check out the code, initialise
/// the backend, validate it, resolve the input variables.
///
/// The runner does one piece of work per step whichever job asked for it, but each job kind has its
/// own messages and its own endpoints. A dispatch queue is named after its message type, so a shared
/// request would put both kinds on one queue; and a reply has to find one saga, which is why an
/// apply's reply cannot be a destroy's.
/// </summary>
public abstract class GetModuleRequestedBase : StepRequestBase;

public abstract class InitRequestedBase : StepRequestBase;

public abstract class ValidateRequestedBase : StepRequestBase;

public abstract class VariablesRequestedBase : StepRequestBase;

public class ApplyGetModuleRequested : GetModuleRequestedBase;
public class ApplyGetModuleCompleted : StepResponseBase;
public class ApplyGetModuleCancelled : StepResponseBase;
public class ApplyGetModuleFaulted : StepFaultedBase;

public class ApplyInitRequested : InitRequestedBase;
public class ApplyInitCompleted : StepResponseBase;
public class ApplyInitCancelled : StepResponseBase;
public class ApplyInitFaulted : StepFaultedBase;

public class ApplyValidateRequested : ValidateRequestedBase;
public class ApplyValidateCompleted : StepResponseBase;
public class ApplyValidateCancelled : StepResponseBase;
public class ApplyValidateFaulted : StepFaultedBase;

public class ApplyVariablesRequested : VariablesRequestedBase;
public class ApplyVariablesCompleted : StepResponseBase;
public class ApplyVariablesCancelled : StepResponseBase;
public class ApplyVariablesFaulted : StepFaultedBase;

public class DestroyGetModuleRequested : GetModuleRequestedBase;
public class DestroyGetModuleCompleted : StepResponseBase;
public class DestroyGetModuleCancelled : StepResponseBase;
public class DestroyGetModuleFaulted : StepFaultedBase;

public class DestroyInitRequested : InitRequestedBase;
public class DestroyInitCompleted : StepResponseBase;
public class DestroyInitCancelled : StepResponseBase;
public class DestroyInitFaulted : StepFaultedBase;

public class DestroyValidateRequested : ValidateRequestedBase;
public class DestroyValidateCompleted : StepResponseBase;
public class DestroyValidateCancelled : StepResponseBase;
public class DestroyValidateFaulted : StepFaultedBase;

public class DestroyVariablesRequested : VariablesRequestedBase;
public class DestroyVariablesCompleted : StepResponseBase;
public class DestroyVariablesCancelled : StepResponseBase;
public class DestroyVariablesFaulted : StepFaultedBase;
