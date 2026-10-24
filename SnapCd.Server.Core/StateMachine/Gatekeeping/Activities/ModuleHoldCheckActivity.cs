// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.ModuleHold;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

/// <summary>
/// Checks again just after a hold that is still live would expire; once it has lapsed, hands the queue back
/// to the dependency check by clearing the held reason.
/// </summary>
public class ModuleHoldCheckActivity : IStateMachineActivity<ModuleSaga, ModuleHoldCheck>
{
    private readonly ModuleHoldStore _holds;
    private readonly ModuleHoldGate _gate;
    private readonly ILogger<ModuleHoldCheckActivity> _logger;

    public ModuleHoldCheckActivity(ModuleHoldStore holds, ModuleHoldGate gate, ILogger<ModuleHoldCheckActivity> logger)
    {
        _holds = holds;
        _gate = gate;
        _logger = logger;
    }

    public async Task Execute(BehaviorContext<ModuleSaga, ModuleHoldCheck> context, IBehavior<ModuleSaga, ModuleHoldCheck> next)
    {
        if (context.Saga.QueuedReason == QueuedReason.HeldBySession)
        {
            var state = await _holds.GetHoldState(context.Saga.OrganizationId, context.Saga.CorrelationId);
            if (state.Held)
            {
                await _gate.ScheduleCheck(context.Saga, state.ExpiresAt!.Value);
                _logger.LogDebug("Module {ModuleId} still held until {ExpiresAt}: checking again then",
                    context.Saga.CorrelationId, state.ExpiresAt);
            }
            else
            {
                context.Saga.QueuedReason = QueuedReason.WaitingOnDependencies;
                _logger.LogInformation("Hold on module {ModuleId} lapsed: releasing its queue", context.Saga.CorrelationId);
            }
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<ModuleSaga, ModuleHoldCheck, TException> context,
        IBehavior<ModuleSaga, ModuleHoldCheck> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("module-hold-check");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
