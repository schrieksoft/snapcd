using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class NamespaceModifiedCompetingConsumer : IConsumer<NamespaceModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public NamespaceModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<NamespaceModifiedEvent> context)
    {
        var triggerBehaviour = _dbContext
            .Namespaces.Where(x => x.Id == context.Message.Id)
            .Select(x => (NamespaceTriggerBehaviour?)x.TriggerBehaviourOnModified)
            .FirstOrDefault();

        if (triggerBehaviour == NamespaceTriggerBehaviour.TriggerAllImmediately)
        {
            var moduleIds = _dbContext.Modules
                .Where(x => x.NamespaceId == context.Message.Id && x.TriggerOnDefinitionChanged)
                .Select(x => x.Id).ToList();

            foreach (var moduleId in moduleIds) await _bus.Publish(new ModuleModifiedTriggerRequested { ModuleId = moduleId, OrganizationId = context.Message.OrganizationId });
        }
    }
}