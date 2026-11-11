// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.Nonsecured.Approvals;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

public class WaitingForApprovalModuleJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase, SagaStateMachineInstance
    where TMessage : class
{
    private readonly ModuleJobRepository _repository;
    private readonly SnapCdDbContext _dbContext;

    public WaitingForApprovalModuleJobActivity(ModuleJobRepository repository, SnapCdDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        await _repository.WaitingForApproval(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            true);

        // The threshold is captured here, where the job parks, because the Module's own value may
        // change while it waits and afterwards.
        var threshold = await DeploymentApprovalThreshold.Resolve(
            context.Saga, context.Saga.ModuleId, context.Saga.OrganizationId, _dbContext);

        await _repository.RecordApprovalDecision(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ApprovalDecision.None,
            threshold);

        // Publish so the Layer-1 ApprovalRequestedCompetingConsumer can fire the ApprovalRecommend
        // mission. Carries the saga's CorrelationId (= ModuleJobId) and ModuleId so the consumer can
        // resolve scope and dispatch via MissionDispatcher.
        await context.Publish(new ModuleJobAwaitingApprovalEvent
        {
            ModuleJobId = context.Saga.CorrelationId,
            ModuleId = context.Saga.ModuleId,
            OrganizationId = context.Saga.OrganizationId
        });

        // Proceed to the next activity
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("waiting-for-approval-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}