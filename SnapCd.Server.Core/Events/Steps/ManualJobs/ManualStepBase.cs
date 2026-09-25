// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.ManualJobs;

/// <summary>Parameters every manual job step needs, beyond those an ordinary job step carries.</summary>
public abstract class ManualStepRequestBase : StepRequestBase
{
    /// <summary>The Module this step is for.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>This Module's root within its own checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }
}

/// <summary>A manual job step's reply.</summary>
public class ManualStepResponseBase : StepResponseBase
{
    /// <summary>Which Module answered.</summary>
    public Guid ModuleId { get; set; }
}

/// <summary>A manual job step that failed, and what broke.</summary>
public class ManualStepFaultedBase : ManualStepResponseBase
{
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }

    /// <summary>True when the server failed to dispatch the step, rather than the runner failing it.</summary>
    public bool IsServerSideError { get; set; }
}
