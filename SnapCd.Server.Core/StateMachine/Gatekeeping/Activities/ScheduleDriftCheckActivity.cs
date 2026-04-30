using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

public class ScheduleDriftCheckActivity<TMessage> :
    IStateMachineActivity<ModuleSaga, TMessage> where TMessage : class
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly QuotaService _quotaService;

    public ScheduleDriftCheckActivity(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        QuotaService quotaService)
    {
        _dbContextFactory = dbContextFactory;
        _quotaService = quotaService;
    }

    public async Task Execute(
        BehaviorContext<ModuleSaga, TMessage> context,
        IBehavior<ModuleSaga, TMessage> next)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var moduleData = await dbContext.Modules
                .Where(m => m.Id == context.Saga.CorrelationId && m.OrganizationId == context.Saga.OrganizationId)
                .Select(m => new
                {
                    m.DriftCheckEnabled,
                    m.DriftCheckIntervalMinutes,
                    NamespaceDefaultDriftCheckEnabled = m.Namespace.DefaultDriftCheckEnabled,
                    NamespaceDefaultDriftCheckIntervalMinutes = m.Namespace.DefaultDriftCheckIntervalMinutes
                })
                .FirstOrDefaultAsync();

            if (moduleData == null)
            {
                Console.WriteLine($"Module {context.Saga.CorrelationId} not found, skipping drift check scheduling");
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            var enabled = moduleData.DriftCheckEnabled ?? moduleData.NamespaceDefaultDriftCheckEnabled ?? false;
            if (!enabled)
            {
                Console.WriteLine($"Drift check not enabled for module {context.Saga.CorrelationId}, skipping");
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            var quotaLimits = await _quotaService.GetQuotaLimitsAsync(context.Saga.OrganizationId);

            var interval = moduleData.DriftCheckIntervalMinutes
                           ?? moduleData.NamespaceDefaultDriftCheckIntervalMinutes
                           ?? quotaLimits?.DefaultDriftCheckIntervalMinutes
                           ?? 1440;

            var minInterval = quotaLimits?.MinDriftCheckIntervalMinutes ?? 720;
            var effectiveInterval = Math.Max(interval, minInterval);

            var consumeContext = context.GetPayload<ConsumeContext>();
            var scheduler = consumeContext.GetPayload<IMessageScheduler>();

            var scheduledMessage = await scheduler.SchedulePublish(
                TimeSpan.FromMinutes(effectiveInterval),
                new DriftCheckScheduled
                {
                    ModuleId = context.Saga.CorrelationId,
                    OrganizationId = context.Saga.OrganizationId
                });

            context.Saga.DriftCheckScheduleTokenId = scheduledMessage.TokenId;

            Console.WriteLine($"Scheduled drift check for module {context.Saga.CorrelationId} in {effectiveInterval} minutes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scheduling drift check for module {context.Saga.CorrelationId}: {ex.Message}");
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<ModuleSaga, TMessage, TException> context,
        IBehavior<ModuleSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("schedule-drift-check");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
