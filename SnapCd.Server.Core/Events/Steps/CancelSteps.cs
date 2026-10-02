// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

public abstract class CancelKillRequestedBase : CorrelationBase
{
    public string? RunnerInstanceName { get; set; }
    public Guid RunnerId { get; set; }
}

public abstract class CancelKillCompletedBase : StepResponseBase;

public class ApplyCancelKillRequested : CancelKillRequestedBase;

public class ApplyCancelKillCompleted : CancelKillCompletedBase;

public class DummyApplyCancelKillCompleted : StepResponseBase;

public class DestroyCancelKillRequested : CancelKillRequestedBase;

public class DestroyCancelKillCompleted : CancelKillCompletedBase;

public class DummyDestroyCancelKillCompleted : StepResponseBase;

public class SplitCancelKillRequested : CancelKillRequestedBase;

public class SplitCancelKillCompleted : CancelKillCompletedBase;

public class DummySplitCancelKillCompleted : StepResponseBase;


public class MoveCancelKillRequested : CancelKillRequestedBase;

public class MoveCancelKillCompleted : CancelKillCompletedBase;

public class DummyMoveCancelKillCompleted : StepResponseBase;

public class ImportCancelKillRequested : CancelKillRequestedBase;

public class ImportCancelKillCompleted : CancelKillCompletedBase;

public class DummyImportCancelKillCompleted : StepResponseBase;

public class RemoveCancelKillRequested : CancelKillRequestedBase;

public class RemoveCancelKillCompleted : CancelKillCompletedBase;

public class DummyRemoveCancelKillCompleted : StepResponseBase;
