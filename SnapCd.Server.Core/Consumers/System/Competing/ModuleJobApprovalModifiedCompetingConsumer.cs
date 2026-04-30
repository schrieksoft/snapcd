using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class ModuleJobApprovalModifiedCompetingConsumer :
    IConsumer<ModuleJobApprovalCreatedEvent>,
    IConsumer<ModuleJobApprovalDeletedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public ModuleJobApprovalModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<ModuleJobApprovalCreatedEvent> context)
    {
        await PublishReevaluation(context.Message.Data.ModuleJobId);
    }

    public async Task Consume(ConsumeContext<ModuleJobApprovalDeletedEvent> context)
    {
        await PublishReevaluation(context.Message.Data.ModuleJobId);
    }

    private async Task PublishReevaluation(Guid moduleJobId)
    {
        var moduleId = await _dbContext.ModuleJobs
            .Where(j => j.Id == moduleJobId)
            .Select(j => j.ModuleId)
            .FirstOrDefaultAsync();

        if (moduleId == default)
            return;

        await _bus.Publish(new ApprovalReevaluationRequestedEvent
        {
            ModuleId = moduleId,
            ModuleJobId = moduleJobId
        });
    }
}
