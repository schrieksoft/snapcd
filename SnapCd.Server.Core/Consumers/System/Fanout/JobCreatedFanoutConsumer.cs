using MassTransit;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class JobCreatedFanoutConsumer : IConsumer<ModuleJobCreatedEvent>
{
    private readonly JobCreatedNotificationService _notificationService;

    public JobCreatedFanoutConsumer(JobCreatedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<ModuleJobCreatedEvent> context)
    {
        await _notificationService.Notify(
            context.Message.Data.Id,
            context.Message.Data.ModuleId
        );
    }
}