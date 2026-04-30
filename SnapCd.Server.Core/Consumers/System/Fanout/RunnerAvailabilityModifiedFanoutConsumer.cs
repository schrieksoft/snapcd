using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class RunnerAvailabilityModifiedFanoutConsumer : IConsumer<RunnerAvailabilityChangedEvent>
{
    private readonly RunnerAvailabilityModifiedNotificationService _notificationService;

    public RunnerAvailabilityModifiedFanoutConsumer(RunnerAvailabilityModifiedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<RunnerAvailabilityChangedEvent> context)
    {
        await _notificationService.Notify(
            context.Message.RunnerId,
            context.Message.RunnerInstanceName
        );
    }
}