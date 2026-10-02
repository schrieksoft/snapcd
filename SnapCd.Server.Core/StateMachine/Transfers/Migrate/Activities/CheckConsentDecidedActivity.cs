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
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;

/// <summary>
/// Asks whether the counterparty has already answered, and says so again if it has. What this half
/// waits for is the answer being recorded, not a message carrying it: the answer can be given before
/// this half has parked, and nothing publishes it a second time.
/// </summary>
public class CheckConsentDecidedActivity(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    ILogger<CheckConsentDecidedActivity> logger)
    : IStateMachineActivity<TransferMigrateSaga>
{
    public async Task Execute(
        BehaviorContext<TransferMigrateSaga> context,
        IBehavior<TransferMigrateSaga> next)
    {
        await Check(context.Saga, context);
        await next.Execute(context).ConfigureAwait(false);
    }

    public async Task Execute<T>(
        BehaviorContext<TransferMigrateSaga, T> context,
        IBehavior<TransferMigrateSaga, T> next)
        where T : class
    {
        await Check(context.Saga, context);
        await next.Execute(context).ConfigureAwait(false);
    }

    private async Task Check(TransferMigrateSaga saga, PipeContext context)
    {
        if (saga.TransferId is not { } transferId) return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var status = await dbContext.Transfers.AsNoTracking()
            .Where(t => t.Id == transferId && t.OrganizationId == saga.OrganizationId)
            .Select(t => (ConsentStatus?)t.ConsentStatus)
            .FirstOrDefaultAsync();

        if (status is not (ConsentStatus.Granted or ConsentStatus.Refused)) return;

        logger.LogDebug(
            "Transfer: Module {ModuleId} parked for an answer that was already {Status}",
            saga.ModuleId, status);

        await context.GetPayload<ConsumeContext>().Publish(new ConsentDecided
        {
            TransferId = transferId,
            ModuleId = saga.CounterpartyModuleId,
            OrganizationId = saga.OrganizationId,
            Granted = status == ConsentStatus.Granted
        });
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TransferMigrateSaga, TException> context,
        IBehavior<TransferMigrateSaga> next)
        where TException : Exception
        => next.Faulted(context);

    public Task Faulted<T, TException>(
        BehaviorExceptionContext<TransferMigrateSaga, T, TException> context,
        IBehavior<TransferMigrateSaga, T> next)
        where T : class
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("check-consent-decided");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
