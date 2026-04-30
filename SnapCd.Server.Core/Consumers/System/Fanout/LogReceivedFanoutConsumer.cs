using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class LogReceivedFanoutConsumer : IConsumer<LogReceivedEvent>
{
    private readonly LogReceivedNotificationService _notificationService;

    public LogReceivedFanoutConsumer(LogReceivedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<LogReceivedEvent> context)
    {
        await _notificationService.Notify(
            context.Message.JobId,
            context.Message.ModuleId
        );
    }
}