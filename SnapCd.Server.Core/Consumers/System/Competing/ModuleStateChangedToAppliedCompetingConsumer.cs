using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.DependencyGraph;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class ModuleStateChangedToAppliedCompetingConsumer : IConsumer<ModuleStateChangedToAppliedEvent>
{
    private readonly ILogger<ModuleStateChangedToAppliedCompetingConsumer> _logger;
    private readonly DependencyGraphServiceFactory _applyServiceFactory;

    public ModuleStateChangedToAppliedCompetingConsumer(
        ILogger<ModuleStateChangedToAppliedCompetingConsumer> logger,
        DependencyGraphServiceFactory applyServiceFactory)
    {
        _logger = logger;
        _applyServiceFactory = applyServiceFactory;
    }

    public async Task Consume(ConsumeContext<ModuleStateChangedToAppliedEvent> context)
    {
        _logger.LogDebug("Module {ModuleId} state changed to Applied, checking dependents",
            context.Message.ModuleId);

        try
        {
            using var applyService = _applyServiceFactory.Create();

            // Find modules that depend on this module for apply operations
            var referencedModuleIds = await applyService.ListModuleIdsForReferencedModule(context.Message.ModuleId);

            foreach (var referencedModuleId in referencedModuleIds)
            {
                _logger.LogDebug("Requesting dependency check for dependent module {DependentModuleId}",
                    referencedModuleId);

                // Request a dependency check for each dependent module
                // Use the same OrganizationId as the event since dependencies are within the same organization
                await context.Publish(new ModuleDependencyCheckRequested
                {
                    ModuleId = referencedModuleId,
                    OrganizationId = context.Message.OrganizationId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ModuleStateChangedToAppliedEvent for module {ModuleId}",
                context.Message.ModuleId);
        }
    }
}