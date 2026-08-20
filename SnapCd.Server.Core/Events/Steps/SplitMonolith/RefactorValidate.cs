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
/// Asks the engine whether the written module directories are valid — init -backend=false plus
/// validate on each, so no credentials and no state are involved. Runs before the diff, and long
/// before any state is pulled.
/// </summary>
public class RefactorValidateRequested : SplitStepRequestBase;

public class RefactorValidateCompleted : StepResponseBase;

public class RefactorValidateFaulted : StepFaultedBase;

public class RefactorValidateCancelled : StepResponseBase;
