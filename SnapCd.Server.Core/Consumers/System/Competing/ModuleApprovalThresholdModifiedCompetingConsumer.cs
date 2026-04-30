using MassTransit;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class ModuleApprovalThresholdModifiedCompetingConsumer : IConsumer<ModuleApprovalThresholdModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public ModuleApprovalThresholdModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<ModuleApprovalThresholdModifiedEvent> context)
    {
        var jobsId = _dbContext.ModuleJobs
            .Where(x => x.ModuleId == context.Message.ModuleId && x.WaitingForApproval == true)
            .Select(x => x.Id).ToList();

        foreach (var jobId in jobsId) await _bus.Publish(new ApprovalReevaluationRequestedEvent { ModuleId = context.Message.ModuleId, ModuleJobId = jobId });
    }
}