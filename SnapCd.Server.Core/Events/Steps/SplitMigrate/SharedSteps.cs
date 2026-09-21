// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.SplitMigrate;

/// <summary>
/// The four steps a split job shares in substance with an ordinary job: the same work on the
/// runner, but their own contracts on the server. A published reply is routed by message type, so
/// one contract for both job kinds would deliver every reply to both authorization paths, and the
/// ordinary one resolves ids only through ApplyJobSaga and DestroyJobSaga.
/// </summary>
public class SplitGetModuleRequested : SplitStepRequestBase;

public class SplitGetModuleCompleted : StepResponseBase;

public class SplitGetModuleFaulted : StepFaultedBase;

public class SplitGetModuleCancelled : StepResponseBase;

public class SplitInitRequested : SplitStepRequestBase;

public class SplitInitCompleted : StepResponseBase;

public class SplitInitFaulted : StepFaultedBase;

public class SplitInitCancelled : StepResponseBase;

public class SplitValidateRequested : SplitStepRequestBase;

public class SplitValidateCompleted : StepResponseBase;

public class SplitValidateFaulted : StepFaultedBase;

public class SplitValidateCancelled : StepResponseBase;

public class SplitPlanRequested : SplitStepRequestBase;

/// <summary>Carries the change count: a split refuses to proceed unless the monolith plans clean.</summary>
public class SplitPlanCompleted : StepResponseBase
{
    public int TotalChangedCount { get; set; }
}

public class SplitPlanFaulted : StepFaultedBase;

public class SplitPlanCancelled : StepResponseBase;
