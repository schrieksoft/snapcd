using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class ModuleApprovalThresholdModifiedFanoutConsumer : IConsumer<ModuleApprovalThresholdModifiedEvent>
{
    private readonly ModuleApprovalThresholdModifiedNotificationService _notificationService;

    public ModuleApprovalThresholdModifiedFanoutConsumer(ModuleApprovalThresholdModifiedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<ModuleApprovalThresholdModifiedEvent> context)
    {
        await _notificationService.Notify(
            context.Message.ModuleId
        );
    }
}