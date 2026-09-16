// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

/// <summary>
/// Publishes ModuleQuiet when nothing is running against the Module. An explicit request is
/// always answered; a job completion answers only while the Module is paused, so a manual job
/// waiting for the pause to finish draining hears it without polling.
/// </summary>
public class AnswerQuiescenceActivity<TMessage> : IStateMachineActivity<ModuleSaga, TMessage> where TMessage : class
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ILogger<AnswerQuiescenceActivity<TMessage>> _logger;

    public AnswerQuiescenceActivity(IDbContextFactory<SnapCdDbContext> dbContextFactory, ILogger<AnswerQuiescenceActivity<TMessage>> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task Execute(BehaviorContext<ModuleSaga, TMessage> context, IBehavior<ModuleSaga, TMessage> next)
    {
        if (context.Message is ModuleQuiescenceRequested || context.Saga.Paused)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var moduleId = context.Saga.CorrelationId;
            var organizationId = context.Saga.OrganizationId;

            var running = await dbContext.ModuleJobs.AnyAsync(j => j.ModuleId == moduleId && j.OrganizationId == organizationId && j.IsCurrent == true)
                          || await dbContext.ManualModuleJobs.AnyAsync(j => j.ModuleId == moduleId && j.OrganizationId == organizationId && j.Status == ExecutionStatus.Running);

            if (!running)
            {
                _logger.LogDebug("Module {ModuleId} is quiet", moduleId);
                await context.Publish(new ModuleQuiet { ModuleId = moduleId, OrganizationId = organizationId });
            }
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<ModuleSaga, TMessage, TException> context, IBehavior<ModuleSaga, TMessage> next) where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("answer-quiescence");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
