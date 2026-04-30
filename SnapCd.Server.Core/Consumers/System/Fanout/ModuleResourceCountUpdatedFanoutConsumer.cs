using MassTransit;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class ModuleResourceCountUpdatedFanoutConsumer : IConsumer<ModuleResourceCountUpdatedEvent>
{
    private readonly ILogger<ModuleResourceCountUpdatedFanoutConsumer> _logger;

    public ModuleResourceCountUpdatedFanoutConsumer(
        ILogger<ModuleResourceCountUpdatedFanoutConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ModuleResourceCountUpdatedEvent> context)
    {
        _logger.LogDebug("Module resource count updated event received for ModuleId: {ModuleId}, invalidating usage cache",
            context.Message.ModuleId);
        return Task.CompletedTask;
    }
}