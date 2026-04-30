using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.DependencyGraph;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class ModuleStateChangedToDestroyedCompetingConsumer : IConsumer<ModuleStateChangedToDestroyedEvent>
{
    private readonly ILogger<ModuleStateChangedToDestroyedCompetingConsumer> _logger;
    private readonly DependencyGraphServiceFactory _destroyServiceFactory;

    public ModuleStateChangedToDestroyedCompetingConsumer(
        ILogger<ModuleStateChangedToDestroyedCompetingConsumer> logger,
        DependencyGraphServiceFactory destroyServiceFactory)
    {
        _logger = logger;
        _destroyServiceFactory = destroyServiceFactory;
    }

    public async Task Consume(ConsumeContext<ModuleStateChangedToDestroyedEvent> context)
    {
        _logger.LogDebug("Module {ModuleId} state changed to Destroyed, checking dependents",
            context.Message.ModuleId);

        try
        {
            using var destroyService = _destroyServiceFactory.Create();

            // Find modules that depend on this module for apply operations
            var definedModuleIds = await destroyService.ListModuleIdsForDefinedModule(context.Message.ModuleId);

            foreach (var definedModuleId in definedModuleIds)
            {
                _logger.LogDebug("Requesting dependency check for dependent module {DependentModuleId}",
                    definedModuleId);

                // Request a dependency check for each dependent module
                // Use the same OrganizationId as the event since dependencies are within the same organization
                await context.Publish(new ModuleDependencyCheckRequested
                {
                    ModuleId = definedModuleId,
                    OrganizationId = context.Message.OrganizationId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ModuleStateChangedToDestroyedEvent for module {ModuleId}",
                context.Message.ModuleId);
        }
    }
}