// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Services.MaintenanceMode;

namespace SnapCd.Server.Core.StateMachine.ManualJobs.Activities;

/// <summary>
/// Whether the runner this job is pinned to is connected, recorded on the saga so the step that
/// follows can park instead of dispatching into nothing.
///
/// A manual job is pinned to one runner instance for its whole life, so a step dispatched while
/// that instance is away is not late, it is lost: nothing answers and the heartbeat eventually
/// fails the job. Parking instead lets the reconnect re-send it.
/// </summary>
public class RunnerConnectedActivity<TSaga, TMessage> : IStateMachineActivity<TSaga, TMessage>
    where TSaga : ManualJobSagaBase
    where TMessage : class
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IMaintenanceModeService _maintenanceMode;

    public RunnerConnectedActivity(SnapCdDbContext dbContext, IMaintenanceModeService maintenanceMode)
    {
        _dbContext = dbContext;
        _maintenanceMode = maintenanceMode;
    }

    public async Task Execute(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        var saga = context.Saga;

        // A window closing parks the job at the same boundary a missing runner does.
        var parked = await _maintenanceMode.IsActiveAsync()
                     || !await _dbContext.RunnerConnections.AnyAsync(rc =>
                         rc.RunnerId == saga.RunnerId &&
                         rc.OrganizationId == saga.OrganizationId &&
                         rc.InstanceName == saga.RunnerInstanceName);

        // Reading only: writing the job row here would contend with the saga's own transaction,
        // which still holds it. The branch that parks writes the flag instead.
        saga.PreviousStateBeforeWaiting = parked ? saga.CurrentState : null;

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("runner-connected");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
