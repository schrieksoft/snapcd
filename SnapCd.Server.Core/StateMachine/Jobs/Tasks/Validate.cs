// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.StateMachine.Jobs;

public partial class JobStateMachine<
    TSaga,
    TRequest,
    TResponseFailed,
    TResponseCompleted,
    TResponseCancelled,
    TPlanRequested,
    TPlanCompleted,
    TPlanCancelled,
    TApplyFromPlanRequested,
    TApplyFromPlanCompleted,
    TApplyFromPlanCancelled>
    where TSaga : JobSagaBase
    where TRequest : ModuleJobEventBase
    where TResponseFailed : ModuleJobEventCompletedBase, new()
    where TResponseCompleted : ModuleJobEventCompletedBase, new()
    where TResponseCancelled : ModuleJobEventCompletedBase, new()
    where TPlanRequested : StepRequestBase, new()
    where TPlanCompleted : PlanCompletedBase
    where TPlanCancelled : StepResponseBase
    where TApplyFromPlanRequested : StepRequestBase, new()
    where TApplyFromPlanCompleted : ApplyResponseBase
    where TApplyFromPlanCancelled : StepResponseBase
{
    // Validate events
    public Event<ValidateCompleted> ValidateCompleted { get; } = null!;
    public Event<ValidateCancelled> ValidateCancelled { get; } = null!;
    public Event<ValidateFaulted> ValidateFaulted { get; } = null!;

    // Validate states
    public State ValidatePending { get; } = null!;
    public State ValidateWaitingForRunner { get; } = null!;

    private void Configure_Validate()
    {
        // Validate events
        Event(() => ValidateCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ValidateCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ValidateFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        CreateStep<ValidateCompleted, ValidateCancelled, ValidateFaulted, VariablesRequested>(
            ValidateWaitingForRunner,
            ValidatePending,
            ValidateCompleted,
            ValidateCancelled,
            ValidateFaulted,
            VariablesWaitingForRunner,
            VariablesPending
        );
    }
}