// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

/// <summary>
/// The probe step: plan this Module with an upstream's changed outputs supplied as the values they
/// will become, and report what that does to its own outputs. It runs in the checkout the Module's
/// own job is already holding, so it has no GetModule, no Init and no runner selection of its own.
///
/// A destroy answers the step without a runner. Its outputs are removed rather than changed, so
/// there is nothing to plan: the consumer marks the Module's edge ModuleDestroyed and completes.
/// </summary>
public abstract class ProbeRequestedBase : StepRequestBase
{
    /// <summary>
    /// The values to plan with, keyed by this Module's own variable names. Carried as encoded text
    /// the whole way: an unknown has no JSON representation, so only planwalker decodes it.
    /// </summary>
    public string InputsJson { get; set; } = null!;

    /// <summary>The wave this probe answers for, which is what its signals are recorded against.</summary>
    public Guid ProbeWaveId { get; set; }
}

public abstract class ProbeCompletedBase : StepResponseBase
{
    /// <summary>planwalker's signals document, absent when it produced none.</summary>
    public string? SignalsJson { get; set; }
}

public abstract class ApplyProbeFaultedBase : StepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.ApplyProbe;
}

public abstract class DestroyProbeFaultedBase : StepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.DestroyProbe;
}

public class ApplyProbeRequested : ProbeRequestedBase;
public class ApplyProbeCompleted : ProbeCompletedBase;
public class ApplyProbeCancelled : StepResponseBase;
public class ApplyProbeFaulted : ApplyProbeFaultedBase;

public class DestroyProbeRequested : ProbeRequestedBase;
public class DestroyProbeCompleted : ProbeCompletedBase;
public class DestroyProbeCancelled : StepResponseBase;
public class DestroyProbeFaulted : DestroyProbeFaultedBase;
