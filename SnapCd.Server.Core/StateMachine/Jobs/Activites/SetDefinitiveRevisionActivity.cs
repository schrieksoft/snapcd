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