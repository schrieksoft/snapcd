using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

public class FailModuleJobActivity<TSaga, TFailedMessage> :
    IStateMachineActivity<TSaga, TFailedMessage>
    where TSaga : JobSagaBase
    where TFailedMessage : class
{
    private readonly ModuleJobRepository _repository;

    public FailModuleJobActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TFailedMessage> context,
        IBehavior<TSaga, TFailedMessage> next)
    {
        // Determine ActualStateHeadline based on job type for failed jobs
        var actualStateHeadline = typeof(TSaga).Name switch
        {
            "ApplyJobSaga" => ActualStateHeadline.ApplyFailed,
            "DestroyJobSaga" => ActualStateHeadline.DestroyFailed,
            _ => (ActualStateHeadline?)null
        };

        // Check if this is a server-side error
        var isServerSideError = false;
        string? errorMessage = null;
        string? stackTrace = null;

        if (context.Message is StepFaultedBase faultedMessage)
        {
            isServerSideError = faultedMessage.IsServerSideError;
            errorMessage = faultedMessage.ErrorMessage;
            stackTrace = faultedMessage.StackTrace;
        }

        if (isServerSideError)
        {
            // Server-side error: save error details to the job
            var failedStep = StepMapper.DetermineStepFromEventType(typeof(TFailedMessage));
            var errorHeader = $"Server error during {failedStep}";
            var fullErrorMessage = !string.IsNullOrEmpty(stackTrace)
                ? $"{errorMessage}\n\nStack Trace:\n{stackTrace}"
                : errorMessage;

            await _repository.FinalizeWithServerError(
                context.Saga.CorrelationId,
                context.Saga.OrganizationId,
                typeof(TFailedMessage).Name,
                DateTimeOffset.UtcNow,
                actualStateHeadline,
                failedStep,
                errorHeader,
                fullErrorMessage);
        }
        else
        {
            // Runner-side error or non-faulted message: use standard finalization
            await _repository.Finalize(
                context.Saga.CorrelationId,
                context.Saga.OrganizationId,
                ExecutionStatus.Failed,
                typeof(TFailedMessage).Name,
                DateTimeOffset.UtcNow,
                null,
                actualStateHeadline);
        }

        // Proceed to the next activity
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TFailedMessage, TException> context,
        IBehavior<TSaga, TFailedMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("fail-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}