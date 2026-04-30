using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class ModuleSagaModifiedFanoutConsumer : IConsumer<ModuleSagaModifiedEvent>
{
    private readonly ModuleSagaModifiedNotificationService _notificationService;

    public ModuleSagaModifiedFanoutConsumer(ModuleSagaModifiedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<ModuleSagaModifiedEvent> context)
    {
        await _notificationService.Notify(
            context.Message.ModuleId
        );
    }
}