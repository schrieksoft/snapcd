using MassTransit;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class ModuleModifiedCompetingConsumer : IConsumer<ModuleModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public ModuleModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<ModuleModifiedEvent> context)
    {
        var triggerOnDefinitionChanged = _dbContext
            .Modules.Where(x => x.Id == context.Message.Id)
            .Select(x => x.TriggerOnDefinitionChanged)
            .FirstOrDefault();

        if (triggerOnDefinitionChanged)
        {
            Console.WriteLine($"Publishing ModuleModifiedTriggerRequested with ModuleId {context.Message.Id}");
            await _bus.Publish(new ModuleModifiedTriggerRequested { ModuleId = context.Message.Id, OrganizationId = context.Message.OrganizationId });
        }
    }
}