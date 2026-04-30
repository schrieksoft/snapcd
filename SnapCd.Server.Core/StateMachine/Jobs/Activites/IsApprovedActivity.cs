using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Entities.Sagas.Base;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

public class NeedsApprovalJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase
    where TMessage : class
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IApprovalPolicy _approvalPolicy;

    public NeedsApprovalJobActivity(SnapCdDbContext dbContext, IApprovalPolicy approvalPolicy)
    {
        _dbContext = dbContext;
        _approvalPolicy = approvalPolicy;
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
        // CE: auto-approve, skip approval workflow
        if (await _approvalPolicy.ShouldAutoApproveAsync(context.Saga.OrganizationId))
        {
            context.Saga.IsApproved = true;
            context.Saga.IsDeclined = false;
            await next.Execute(context).ConfigureAwait(false);
            return;
        }

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
