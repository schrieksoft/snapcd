using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class StackModifiedCompetingConsumer : IConsumer<StackUpdatedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public StackModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<StackUpdatedEvent> context)
    {
        var triggerBehaviour = _dbContext
            .Stacks.Where(x => x.Id == context.Message.Data.Id)
            .Select(x => (StackTriggerBehaviour?)x.TriggerBehaviourOnModified)
            .FirstOrDefault();

        if (triggerBehaviour == StackTriggerBehaviour.TriggerAllImmediately)
        {
            var moduleIds = _dbContext.Modules
                .Include(x => x.Namespace)
                .Where(x => x.Namespace.StackId == context.Message.Data.Id && x.TriggerOnDefinitionChanged)
                .Select(x => x.Namespace.StackId).ToList();

            foreach (var moduleId in moduleIds) await _bus.Publish(new ModuleModifiedTriggerRequested { ModuleId = moduleId, OrganizationId = context.Message.Data.Id });
        }
    }
}