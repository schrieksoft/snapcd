//using MassTransit;

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

public class ApprovalTimeoutModuleJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase, SagaStateMachineInstance
    where TMessage : class
{
    private readonly ModuleJobRepository _repository;

    public ApprovalTimeoutModuleJobActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        
        // Determine ActualStateHeadline based on job type for cancelled jobs
        var actualStateHeadline = typeof(TSaga).Name switch
        {
            "ApplyJobSaga" => ActualStateHeadline.ApplyNotApproved,
            "DestroyJobSaga" => ActualStateHeadline.DestroyNotApproved,
            _ => (ActualStateHeadline?)null
        };
        
        await _repository.Finalize(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ExecutionStatus.NotApproved,
            typeof(TMessage).Name,
            DateTimeOffset.UtcNow,
            null,
            actualStateHeadline);


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
        context.CreateScope("approval-timeout-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}