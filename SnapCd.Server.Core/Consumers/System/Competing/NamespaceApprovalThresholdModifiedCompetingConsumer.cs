using MassTransit;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class NamespaceApprovalThresholdModifiedCompetingConsumer : IConsumer<NamespaceApprovalThresholdModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public NamespaceApprovalThresholdModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<NamespaceApprovalThresholdModifiedEvent> context)
    {
        var moduleIds = _dbContext.Modules
            .Where(x => x.NamespaceId == context.Message.NamespaceId)
            .Select(x => x.Id).ToList();

        foreach (var moduleId in moduleIds) await _bus.Publish(new ModuleApprovalThresholdModifiedEvent { ModuleId = moduleId });
    }
}