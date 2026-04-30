using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

public class SetDefinitiveRevisionActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase
    where TMessage : GetDefinitiveRevisionCompleted
{
    private readonly ModuleJobRepository _repository;
    private readonly SnapCdDbContext _dbContext;

    public SetDefinitiveRevisionActivity(ModuleJobRepository repository, SnapCdDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        // Update the ModuleJob with the DefinitiveRevision from the saga
        var moduleJob = await _repository.Get(context.Saga.CorrelationId, context.Saga.OrganizationId);
        moduleJob.DefinitiveRevision = context.Message.DefinitiveRevision;
        await _repository.ExecuteUpdate(moduleJob);

        var moduleSaga = await _dbContext.ModuleSagas
            .FirstOrDefaultAsync(s => s.CorrelationId == moduleJob.ModuleId);
        
        if (moduleSaga != null)
        {
            moduleSaga.DesiredDefinitiveRevision = context.Saga.DefinitiveRevision;
            await _dbContext.SaveChangesAsync();
        }

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
        context.CreateScope("set-definitive-revision");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}