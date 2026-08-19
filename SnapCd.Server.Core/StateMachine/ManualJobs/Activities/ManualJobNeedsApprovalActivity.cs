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

namespace SnapCd.Server.Core.StateMachine.ManualJobs.Activities;

/// <summary>
/// Resolves whether a manual job has enough approvals. Parallel to NeedsApprovalJobActivity, which
/// reads ModuleJobApprovals and switches on the deployment saga types. The threshold is supplied by
/// the concrete job type rather than resolved here, so a second manual job brings its own.
/// </summary>
public abstract class ManualJobNeedsApprovalActivity<TSaga, TMessage> : IStateMachineActivity<TSaga, TMessage>
    where TSaga : ManualJobSagaBase
    where TMessage : class
{
    private readonly SnapCdDbContext _dbContext;

    protected ManualJobNeedsApprovalActivity(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Approvals this job type requires, resolved from the Module then its Namespace.</summary>
    protected abstract Task<int> ResolveThreshold(Guid moduleId, Guid organizationId, SnapCdDbContext dbContext);

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        var threshold = await ResolveThreshold(context.Saga.ModuleId, context.Saga.OrganizationId, _dbContext);

        var approvals = await _dbContext.ManualModuleJobApprovals
            .Where(x => x.ManualModuleJobId == context.Saga.CorrelationId
                        && x.OrganizationId == context.Saga.OrganizationId)
            .ToListAsync();

        var isDeclined = approvals.Any(x => x.Declined);

        context.Saga.IsDeclined = isDeclined;
        context.Saga.IsApproved = !isDeclined && approvals.Count >= threshold;

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context) => context.CreateScope("manual-job-needs-approval");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
