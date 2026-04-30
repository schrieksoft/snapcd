using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class ModuleStateModifiedFanoutConsumer : IConsumer<ModuleStateModifiedEvent>
{
    private readonly ModuleStateModifiedNotificationService _notificationService;

    public ModuleStateModifiedFanoutConsumer(ModuleStateModifiedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<ModuleStateModifiedEvent> context)
    {
        await _notificationService.Notify(
            context.Message.ModuleId
        );
    }
}