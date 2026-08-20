// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.SplitMonolith;

/// <summary>
/// Asserts the plan Plan just wrote is empty. Kept as its own step rather than folded into Plan
/// so the failure names what was actually wrong: a plan that errored and a plan that was not empty
/// are different outcomes.
/// </summary>
public class PlanEmptyVerifyRequested : SplitStepRequestBase;

public class PlanEmptyVerifyCompleted : StepResponseBase;

public class PlanEmptyVerifyFaulted : StepFaultedBase;

public class PlanEmptyVerifyCancelled : StepResponseBase;
