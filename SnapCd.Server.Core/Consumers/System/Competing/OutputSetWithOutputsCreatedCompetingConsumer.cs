using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.Repository.Organization;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class OutputSetWithOutputsCreatedCompetingConsumer : IConsumer<OutputSetWithOutputsCreatedEvent>
{
    private readonly IBus _bus;
    private readonly SnapCdDbContext _dbContext;

    public OutputSetWithOutputsCreatedCompetingConsumer(
        IBus bus,
        SnapCdDbContext dbContext
    )
    {
        _bus = bus;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<OutputSetWithOutputsCreatedEvent> context)
    {
        // OutputSet-based queries: filter by Variable.Name matching changed outputs
        // Trigger if module has no VariableSet (trigger by default) or has matching Variable names
        var modulesIdsParamFromOutputSets = ModuleIdsFromOutputSetsQuery<ModuleParamFromOutputSet>(
            context.Message.Data.ModuleId,
            context.Message.OrganizationId,
            context.Message.Data.CreatedOrUpdatedOutputs);

        // Output-based queries: filter by specific OutputName in changed outputs list
        var modulesIdsParamFromOutputs = ModuleIdsFromOutputsQuery<ModuleParamFromOutput>(
            context.Message.Data.ModuleId,
            context.Message.OrganizationId,
            context.Message.Data.CreatedOrUpdatedOutputs);
        
        var modulesIdsEnvVarFromOutputs = ModuleIdsFromOutputsQuery<ModuleEnvVarFromOutput>(
            context.Message.Data.ModuleId,
            context.Message.OrganizationId,
            context.Message.Data.CreatedOrUpdatedOutputs);
        
        var moduleIds = modulesIdsParamFromOutputSets
            .Concat(modulesIdsParamFromOutputs)
            .Concat(modulesIdsEnvVarFromOutputs)
            .Distinct()
            .ToList();

        foreach (var moduleId in moduleIds)
            await _bus.Publish(new GatekeepingJobRequested
            {
                ModuleId = moduleId,
                OrganizationId = context.Message.OrganizationId,
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                SetNewDesiredState = false
            }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); });
    }
    
    
    private IQueryable<Guid> ModuleIdsFromOutputSetsQuery<TEntity>(Guid outputModuleId, Guid organizationId, List<string> createdOrUpdatedOutputs)
        where TEntity: ModuleInputFromOutputSet
    {
        return _dbContext.Set<TEntity>()
            .Where(x =>
                x.OutputModuleId == outputModuleId &&
                x.Module.Namespace.StackId == x.OutputModule.Namespace.StackId &&
                x.Module.TriggerOnUpstreamOutputChanged &&
                x.OrganizationId == organizationId
            )
            .Select(x => x.ModuleId)
            .Distinct()
            .Where(moduleId =>
                // No VariableSet exists -> trigger by default
                !_dbContext.VariableSets.Any(vs => vs.ModuleId == moduleId && vs.OrganizationId == organizationId)
                ||
                // Latest VariableSet has matching variable name
                _dbContext.VariableSets
                    .Where(vs => vs.ModuleId == moduleId && vs.OrganizationId == organizationId)
                    .OrderByDescending(vs => vs.Timestamp)
                    .Take(1)
                    .SelectMany(vs => vs.Variables)
                    .Any(v => createdOrUpdatedOutputs.Contains(v.Name))
            );

    }
    
    private IQueryable<Guid> ModuleIdsFromOutputsQuery<TEntity>(Guid outputModuleId, Guid organizationId, List<string> createdOrUpdatedOutputs)
        where TEntity: ModuleInputFromOutput
    {
        return _dbContext.Set<TEntity>()
            .Where(x =>
                x.OutputModuleId == outputModuleId &&
                x.Module.Namespace.StackId == x.OutputModule.Namespace.StackId &&
                x.Module.TriggerOnUpstreamOutputChanged &&
                x.OrganizationId == organizationId &&
                createdOrUpdatedOutputs.Contains(x.OutputName)
            )
            .Select(x => x.Module.Id);

    }


}