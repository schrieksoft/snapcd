// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Entities.Sagas.Base;

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

    private int GetThreshold(TSaga saga, Thresholds thresholds)
    {
        switch (saga)
        {
            case ApplyJobSaga:
                return thresholds.ApplyApprovalThreshold;
            case DestroyJobSaga:
                return thresholds.DestroyApprovalThreshold;
            default:
                throw new NotImplementedException($"{nameof(NeedsApprovalJobActivity<TSaga, TMessage>)} not implemented for TSaga of type {saga.GetType().Name}");
        }
    }

    private class Thresholds
    {
        public int ApplyApprovalThreshold { get; set; }
        public int DestroyApprovalThreshold { get; set; }
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        var thresholds = _dbContext.Modules
            .Include(x => x.Namespace)
            .Where(x => x.Id == context.Saga.ModuleId && x.OrganizationId == context.Saga.OrganizationId)
            .Select(x => new Thresholds
            {
                ApplyApprovalThreshold = x.ApplyApprovalThreshold ?? x.Namespace.DefaultApplyApprovalThreshold ?? 0,
                DestroyApprovalThreshold = x.DestroyApprovalThreshold ?? x.Namespace.DefaultDestroyApprovalThreshold ?? 0
            })
            .Single();

        var threshold = GetThreshold(context.Saga, thresholds);

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
