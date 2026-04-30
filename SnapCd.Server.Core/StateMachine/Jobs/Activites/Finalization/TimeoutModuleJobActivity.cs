//using MassTransit;

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

public class TimeoutModuleJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, HeartbeatFailed>
    where TSaga : JobSagaBase, SagaStateMachineInstance
    where TMessage : class
{
    private readonly ModuleJobRepository _repository;

    public TimeoutModuleJobActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, HeartbeatFailed> context,
        IBehavior<TSaga, HeartbeatFailed> next)
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
            ExecutionStatus.Failed,
            typeof(TMessage).Name,
            DateTimeOffset.UtcNow,
            null,
            actualStateHeadline);


        // Proceed to the next activity
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, HeartbeatFailed, TException> context,
        IBehavior<TSaga, HeartbeatFailed> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("timeout-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}