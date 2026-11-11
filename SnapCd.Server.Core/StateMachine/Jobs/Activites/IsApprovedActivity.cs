// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Services.Nonsecured.Approvals;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

public class NeedsApprovalJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase
    where TMessage : class
{
    private readonly SnapCdDbContext _dbContext;
    private readonly ILogger<NeedsApprovalJobActivity<TSaga, TMessage>> _logger;

    public NeedsApprovalJobActivity(
        SnapCdDbContext dbContext,
        ILogger<NeedsApprovalJobActivity<TSaga, TMessage>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        var threshold = await DeploymentApprovalThreshold.Resolve(
            context.Saga, context.Saga.ModuleId, context.Saga.OrganizationId, _dbContext);

        var approvals = _dbContext.ModuleJobApprovals
            .Where(x => x.ModuleJobId == context.Saga.CorrelationId)
            .ToList();

        var isApproved = false;
        var isDeclined = approvals.Any(x => x.Declined);

        if (!isDeclined) isApproved = approvals.Count() >= threshold;

        context.Saga.IsApproved = isApproved;
        context.Saga.IsDeclined = isDeclined;

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
        context.CreateScope("needs-approval-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
