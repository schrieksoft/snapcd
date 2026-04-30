using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class SourceRefreshCompletedCompetingConsumer : IConsumer<SourceRefreshCompleted>
{
    private readonly SnapCdDbContext _dbContext;

    public SourceRefreshCompletedCompetingConsumer(
        SnapCdDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<SourceRefreshCompleted> context)
    {
        var modules = _dbContext.Modules
            .Include(x => x.Runner)
            .Include(x => x.ModuleSaga)
            .Where(x => x.SourceUrl == context.Message.SourceUrl &&
                        x.SourceRevision == context.Message.SourceRevision &&
                        x.SourceType == context.Message.SourceType &&
                        x.TriggerOnSourceChanged
            )
            .Where(x =>
                // Check if the ModuleSaga's desired definitive revision is different from the new revision
                x.ModuleSaga == null ||
                x.ModuleSaga.DesiredDefinitiveRevision != context.Message.DefinitiveRevision
            )
            .Select(x => new
            {
                x.Id,
                x.OrganizationId
            })
            .ToList();

        foreach (var module in modules)
            await context.Publish(new GatekeepingJobRequested
            {
                ModuleId = module.Id,
                OrganizationId = module.OrganizationId,
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                SetNewDesiredState = false,
                DefinitiveRevision = context.Message.DefinitiveRevision
            }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); });
    }
}