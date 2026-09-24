// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;

/// <summary>
/// Whether the counterparty has published every output this Module's plan consumes. Read from its
/// latest output set, so an output that later disappears parks the job again rather than being
/// remembered as once-seen.
/// </summary>
public class TransferOutputsAvailableActivity<TMessage>
    : IStateMachineActivity<TransferMigrateSaga, TMessage>
    where TMessage : class
{
    private readonly SnapCdDbContext _dbContext;

    public TransferOutputsAvailableActivity(SnapCdDbContext dbContext) => _dbContext = dbContext;

    public async Task Execute(
        BehaviorContext<TransferMigrateSaga, TMessage> context,
        IBehavior<TransferMigrateSaga, TMessage> next)
    {
        context.Saga.HasOutputs = await Evaluate(context.Saga);

        await next.Execute(context).ConfigureAwait(false);
    }

    private async Task<bool> Evaluate(TransferMigrateSaga saga)
    {
        if (string.IsNullOrWhiteSpace(saga.NeedsOutputsJson)) return true;

        var needed = JsonSerializer.Deserialize<List<string>>(saga.NeedsOutputsJson);
        if (needed is not { Count: > 0 }) return true;

        var producerId = saga.CounterpartyModuleId;

        var latest = await _dbContext.OutputSets.AsNoTracking()
            .Where(o => o.ModuleId == producerId && o.OrganizationId == saga.OrganizationId)
            .OrderByDescending(o => o.Timestamp)
            .Select(o => o.Outputs.Select(x => x.Name).ToList())
            .FirstOrDefaultAsync();

        return latest != null && needed.All(latest.Contains);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TransferMigrateSaga, TMessage, TException> context,
        IBehavior<TransferMigrateSaga, TMessage> next)
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("transfer-outputs-available");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
