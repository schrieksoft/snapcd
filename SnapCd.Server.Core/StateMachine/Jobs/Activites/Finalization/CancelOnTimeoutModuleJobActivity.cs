//using MassTransit;

using MassTransit;
using MassTransit.Contracts;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

public class CancelOnTimeoutModuleJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, RequestTimeoutExpired<TMessage>>
    where TSaga : JobSagaBase, SagaStateMachineInstance
    where TMessage : class
{
    private readonly ModuleJobRepository _repository;

    public CancelOnTimeoutModuleJobActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, RequestTimeoutExpired<TMessage>> context,
        IBehavior<TSaga, RequestTimeoutExpired<TMessage>> next)
    {
        // Determine ActualStateHeadline based on job type for cancelled jobs
        var actualStateHeadline = typeof(TSaga).Name switch
        {
            "ApplyJobSaga" => ActualStateHeadline.ApplyCancelled,
            "DestroyJobSaga" => ActualStateHeadline.DestroyCancelled,
            _ => (ActualStateHeadline?)null
        };
        
        await _repository.Finalize(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ExecutionStatus.Cancelled,
            typeof(TMessage).Name,
            DateTimeOffset.UtcNow,
            null,
            actualStateHeadline);


        // Proceed to the next activity
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, RequestTimeoutExpired<TMessage>, TException> context,
        IBehavior<TSaga, RequestTimeoutExpired<TMessage>> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("cancel-on-timeout-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}