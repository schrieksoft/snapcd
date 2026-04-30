using MassTransit;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class JobUpdatedFanoutConsumer : IConsumer<ModuleJobUpdatedEvent>
{
    private readonly JobUpdatedNotificationService _notificationService;

    public JobUpdatedFanoutConsumer(JobUpdatedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<ModuleJobUpdatedEvent> context)
    {
        await _notificationService.Notify(
            context.Message.Data.Id,
            context.Message.Data.ModuleId,
            context.Message.Data.Status
        );
    }
}