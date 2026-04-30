using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class ModuleJobApprovalModifiedFanoutConsumer :
    IConsumer<ModuleJobApprovalCreatedEvent>,
    IConsumer<ModuleJobApprovalUpdatedEvent>,
    IConsumer<ModuleJobApprovalDeletedEvent>
{
    private readonly ModuleJobApprovalModifiedNotificationService _notificationService;
    private readonly SnapCdDbContext _dbContext;

    public ModuleJobApprovalModifiedFanoutConsumer(
        ModuleJobApprovalModifiedNotificationService notificationService,
        SnapCdDbContext dbContext)
    {
        _notificationService = notificationService;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<ModuleJobApprovalCreatedEvent> context)
    {
        await NotifyForModuleJob(context.Message.Data.ModuleJobId);
    }

    public async Task Consume(ConsumeContext<ModuleJobApprovalUpdatedEvent> context)
    {
        await NotifyForModuleJob(context.Message.Data.ModuleJobId);
    }

    public async Task Consume(ConsumeContext<ModuleJobApprovalDeletedEvent> context)
    {
        await NotifyForModuleJob(context.Message.Data.ModuleJobId);
    }

    private async Task NotifyForModuleJob(Guid moduleJobId)
    {
        var moduleId = await _dbContext.ModuleJobs
            .Where(j => j.Id == moduleJobId)
            .Select(j => j.ModuleId)
            .FirstOrDefaultAsync();

        if (moduleId == default)
            return;

        await _notificationService.Notify(moduleJobId, moduleId);
    }
}
