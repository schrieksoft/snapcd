// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.Transfer;

/// <summary>
/// Parameters every transfer step needs. Transfer has its own step contracts rather than reusing the
/// deployment ones because two participants run the same task under one correlation id: without a
/// Module id on the message the saga cannot tell which side answered.
/// </summary>
public abstract class TransferStepRequestBase : StepRequestBase
{
    /// <summary>Which participant this step is for. Every transfer step carries it.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>This participant's root within its own checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }
}

/// <summary>A transfer step's reply, naming the participant that produced it.</summary>
public class TransferStepResponseBase : StepResponseBase
{
    /// <summary>Which participant answered.</summary>
    public Guid ModuleId { get; set; }
}

/// <summary>A transfer step that failed, naming the participant and what broke.</summary>
public class TransferStepFaultedBase : TransferStepResponseBase
{
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
}
