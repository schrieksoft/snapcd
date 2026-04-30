using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Modules;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleRepositorySettings> options)
{
    public ModuleRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleRepository : GenericNamespaceChildRepository<Module, ModuleReadDto, ModuleCreatedEvent, ModuleUpdatedEvent, ModuleDeletedEvent, ModuleRepositorySettings>
{
    public ModuleRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<ModuleRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleReadDto MapToDto(Module entity)
    {
        return ModuleMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(Module entity)
    {
        var currentCount = await DbContext.Modules
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleQuota), currentCount);
    }

    protected override List<object> AdditionalCreateMessages(Module entity)
    {
        var messages = new List<object>();
        messages.Add(new ModuleModifiedEvent { Id = entity.Id, OrganizationId = entity.OrganizationId });
        messages.Add(new ModuleSagaModifiedEvent { ModuleId = entity.Id, OrganizationId = entity.OrganizationId });
        return messages;
    }

    protected override List<object> AdditionalUpdateMessages(Module entity)
    {
        var messages = new List<object>();
        messages.Add(new ModuleModifiedEvent { Id = entity.Id, OrganizationId = entity.OrganizationId });
        return messages;
    }

    public override async Task<Module> ExecuteCreate(Module entity)
    {
        if (entity.Engine == StateManagementEngine.Pulumi)
            await ValidatePulumiFeatureEnabled(entity.OrganizationId);

        // Create required sagas before creating the module
        entity.ModuleSaga = new ModuleSaga
        {
            CorrelationId = entity.Id,
            OrganizationId = entity.OrganizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleStateMachine.Gatekeeping),
            DesiredStateHeadline = DesiredStateHeadline.Applied,
            QueuedDesiredStateHeadline = null
        };

        entity.ModuleModifiedSaga = new ModuleModifiedSaga
        {
            CorrelationId = entity.Id,
            OrganizationId = entity.OrganizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleModifiedStateMachine.Idle),
            LastUpdated = null,
            TimeoutTokenId = null
        };

        return await base.ExecuteCreate(entity);
    }

    public override async Task<Module> ExecuteUpdate(Module entity)
    {
        // First get the existing module to check for namespace changes
        var existingModule = await Get(entity.Id, entity.OrganizationId);

        if (entity.Engine == StateManagementEngine.Pulumi && existingModule.Engine != StateManagementEngine.Pulumi)
            await ValidatePulumiFeatureEnabled(entity.OrganizationId);

        // Check if the namespace is changing
        if (existingModule.NamespaceId != entity.NamespaceId) await ValidateNamespaceChange(existingModule, entity.NamespaceId, entity.OrganizationId);

        // Proceed with the base update
        var updated = await base.ExecuteUpdate(entity);

        // Publish approval threshold event if changed
        if (updated.ApplyApprovalThreshold != existingModule.ApplyApprovalThreshold ||
            updated.DestroyApprovalThreshold != existingModule.DestroyApprovalThreshold)
            await EnqueueOrPublish(() => Bus.Publish(new ModuleApprovalThresholdModifiedEvent
            {
                ModuleId = updated.Id
            }, context => { context.TimeToLive = TimeSpan.FromSeconds(60); }));

        return updated;
    }

    public override async Task ExecuteDelete(Guid id, Guid organizationId)
    {
        // Get all secrets scoped to this module that we'll need to delete
        var secretsToDelete = await DbContext.ModuleSecrets
            .Where(s => s.ModuleId == id && s.Module.OrganizationId == organizationId)
            .ToListAsync();

        if (secretsToDelete.Any())
        {
            var secretIds = secretsToDelete.Select(s => s.Id).ToList();

            // First delete all ModuleParamFromSecret and ModuleEnvVarFromSecret records that reference these secrets
            // This must be done before deleting the secrets to avoid FK constraint violations
            var moduleParamsFromSecret = await DbContext.ModuleParamFromSecrets
                .Where(mi => secretIds.Contains(mi.SecretId) && mi.Module.OrganizationId == organizationId)
                .ToListAsync();
            var moduleEnvVarsFromSecret = await DbContext.ModuleEnvVarFromSecrets
                .Where(mi => secretIds.Contains(mi.SecretId) && mi.Module.OrganizationId == organizationId)
                .ToListAsync();

            DbContext.ModuleParamFromSecrets.RemoveRange(moduleParamsFromSecret);
            DbContext.ModuleEnvVarFromSecrets.RemoveRange(moduleEnvVarsFromSecret);

            DbContext.ModuleSecrets.RemoveRange(secretsToDelete);
        }

        // Now proceed with normal module deletion (which will cascade to other entities)
        await base.ExecuteDelete(id, organizationId);
    }

    public async Task<Module> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var module = await DbContext.Modules
            .Where(m => m.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Name == name && i.NamespaceId == namespaceId);

        if (module == null) throw new EntityNotFoundException($"Module with name {name} not found.");

        return module;
    }

    public async Task<Module> Get(string stackName, string namespaceName, string moduleName, Guid organizationId)
    {
        var module = await DbContext.Modules
            .Include(x => x.Namespace)
            .ThenInclude(x => x.Stack)
            .Where(m => m.OrganizationId == organizationId)
            .SingleOrDefaultAsync(x => x.Name == moduleName && x.Namespace.Name == namespaceName && x.Namespace.Stack.Name == stackName);

        if (module == null)
            throw new EntityNotFoundException($"Module could not be found following search parameters: moduleName: {moduleName}, namespaceName: {namespaceName}, stackName: {stackName}.");

        return module;
    }

    public async Task<ModuleRunnerSelectionDto> GetRunnerSelection(Guid moduleId, Guid organizationId)
    {
        return await DbContext.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Include(m => m.Runner)
            .Select(m => new ModuleRunnerSelectionDto
            {
                RunnerName = m.Runner.Name,
                RunnerInstanceName = m.RunnerInstanceName
            })
            .SingleAsync();
    }

    protected override async Task SetServicePrincipalOwner(Guid id, Guid organizationId, Guid servicePrincipalId)
    {
        DbContext.ServicePrincipalModuleRoleAssignments.Add(new ServicePrincipalModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = id,
            ServicePrincipalId = servicePrincipalId,
            RoleName = ModuleRole.Owner
        });
    }

    protected override async Task SetUserOwner(Guid id, Guid organizationId, Guid userId)
    {
        DbContext.UserModuleRoleAssignments.Add(new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = id,
            UserId = userId,
            RoleName = ModuleRole.Owner
        });
    }


    private async Task ValidatePulumiFeatureEnabled(Guid organizationId)
    {
        var accepted = await DbContext.PreviewFeatureAcceptances
            .AnyAsync(p => p.OrganizationId == organizationId && p.PreviewFeature == PreviewFeature.Pulumi);

        if (!accepted)
            throw new PreviewFeatureNotEnabledException(
                "Cannot set Engine to Pulumi because the organization has not opted into the Pulumi preview feature.");
    }

    private async Task ValidateNamespaceChange(Module existingModule, Guid newNamespaceId, Guid organizationId)
    {
        // Get the stack of the new namespace
        var newNamespaceStack = await DbContext.Namespaces
            .Where(n => n.Id == newNamespaceId)
            .Select(n => n.StackId)
            .FirstOrDefaultAsync();

        if (newNamespaceStack == Guid.Empty) throw new InvalidStackReferenceException($"Namespace with ID {newNamespaceId} not found");

        // Check for cross-stack references in ModuleParamFromOutput entities
        var paramFromOutputCrossStackReferences = await DbContext.ModuleParamFromOutputs
            .Where(p => p.ModuleId == existingModule.Id)
            .Join(DbContext.Modules,
                p => p.OutputModuleId,
                m => m.Id,
                (p, m) => new { Input = p, OutputModule = m })
            .Join(DbContext.Namespaces,
                pm => pm.OutputModule.NamespaceId,
                n => n.Id,
                (pm, n) => new { pm.Input, OutputStackId = n.StackId })
            .Where(x => x.OutputStackId != newNamespaceStack)
            .Select(x => new { x.Input.Name, x.Input.OutputModuleId, x.OutputStackId })
            .ToListAsync();

        // Check for cross-stack references in ModuleEnvVarFromOutput entities  
        var envVarFromOutputCrossStackReferences = await DbContext.ModuleEnvVarFromOutputs
            .Where(e => e.ModuleId == existingModule.Id)
            .Join(DbContext.Modules,
                e => e.OutputModuleId,
                m => m.Id,
                (e, m) => new { Input = e, OutputModule = m })
            .Join(DbContext.Namespaces,
                em => em.OutputModule.NamespaceId,
                n => n.Id,
                (em, n) => new { em.Input, OutputStackId = n.StackId })
            .Where(x => x.OutputStackId != newNamespaceStack)
            .Select(x => new { x.Input.Name, x.Input.OutputModuleId, x.OutputStackId })
            .ToListAsync();

        // Check for cross-stack references in ModuleParamFromOutputSet entities
        var paramFromOutputSetCrossStackReferences = await DbContext.ModuleParamFromOutputSets
            .Where(p => p.ModuleId == existingModule.Id)
            .Join(DbContext.Modules,
                p => p.OutputModuleId,
                m => m.Id,
                (p, m) => new { Input = p, OutputModule = m })
            .Join(DbContext.Namespaces,
                pm => pm.OutputModule.NamespaceId,
                n => n.Id,
                (pm, n) => new { pm.Input, OutputStackId = n.StackId })
            .Where(x => x.OutputStackId != newNamespaceStack)
            .Select(x => new { x.Input.Name, x.Input.OutputModuleId, x.OutputStackId })
            .ToListAsync();

        // Check for cross-namespace references in ModuleParamFromNamespace entities
        var paramFromNamespaceCrossNamespaceReferences = await DbContext.ModuleParamFromNamespaces
            .Where(p => p.ModuleId == existingModule.Id)
            .Join(DbContext.NamespaceInputs,
                p => p.NamespaceInputId,
                ni => ni.Id,
                (p, ni) => new { Input = p, NamespaceInput = ni })
            .Where(x => x.NamespaceInput.NamespaceId != newNamespaceId)
            .Select(x => new { x.Input.Name, x.NamespaceInput.NamespaceId })
            .ToListAsync();

        // Check for cross-namespace references in ModuleEnvVarFromNamespace entities
        var envVarFromNamespaceCrossNamespaceReferences = await DbContext.ModuleEnvVarFromNamespaces
            .Where(e => e.ModuleId == existingModule.Id)
            .Join(DbContext.NamespaceInputs,
                e => e.NamespaceInputId,
                ni => ni.Id,
                (e, ni) => new { Input = e, NamespaceInput = ni })
            .Where(x => x.NamespaceInput.NamespaceId != newNamespaceId)
            .Select(x => new { x.Input.Name, x.NamespaceInput.NamespaceId })
            .ToListAsync();

        // Combine all cross-stack references
        var allCrossStackReferences = paramFromOutputCrossStackReferences
            .Concat(envVarFromOutputCrossStackReferences)
            .Concat(paramFromOutputSetCrossStackReferences)
            .ToList();

        // Combine all cross-namespace references
        var allCrossNamespaceReferences = paramFromNamespaceCrossNamespaceReferences
            .Concat(envVarFromNamespaceCrossNamespaceReferences)
            .ToList();

        // Check for incoming references - modules in different stacks that reference this module as OutputModuleId
        var incomingParamFromOutputReferences = await DbContext.ModuleParamFromOutputs
            .Where(p => p.OutputModuleId == existingModule.Id)
            .Join(DbContext.Modules,
                p => p.ModuleId,
                m => m.Id,
                (p, m) => new { Input = p, ConsumerModule = m })
            .Join(DbContext.Namespaces,
                pm => pm.ConsumerModule.NamespaceId,
                n => n.Id,
                (pm, n) => new { pm.Input, ConsumerStackId = n.StackId, pm.ConsumerModule.Name })
            .Where(x => x.ConsumerStackId != newNamespaceStack)
            .Select(x => new { x.Input.Id, x.Input.Name, ConsumerModuleId = x.Input.ModuleId, ConsumerModuleName = x.Name, x.ConsumerStackId, ReferenceType = "ModuleParamFromOutput" })
            .ToListAsync();

        var incomingEnvVarFromOutputReferences = await DbContext.ModuleEnvVarFromOutputs
            .Where(e => e.OutputModuleId == existingModule.Id)
            .Join(DbContext.Modules,
                e => e.ModuleId,
                m => m.Id,
                (e, m) => new { Input = e, ConsumerModule = m })
            .Join(DbContext.Namespaces,
                em => em.ConsumerModule.NamespaceId,
                n => n.Id,
                (em, n) => new { em.Input, ConsumerStackId = n.StackId, em.ConsumerModule.Name })
            .Where(x => x.ConsumerStackId != newNamespaceStack)
            .Select(x => new { x.Input.Id, x.Input.Name, ConsumerModuleId = x.Input.ModuleId, ConsumerModuleName = x.Name, x.ConsumerStackId, ReferenceType = "ModuleEnvVarFromOutput" })
            .ToListAsync();

        var incomingParamFromOutputSetReferences = await DbContext.ModuleParamFromOutputSets
            .Where(p => p.OutputModuleId == existingModule.Id)
            .Join(DbContext.Modules,
                p => p.ModuleId,
                m => m.Id,
                (p, m) => new { Input = p, ConsumerModule = m })
            .Join(DbContext.Namespaces,
                pm => pm.ConsumerModule.NamespaceId,
                n => n.Id,
                (pm, n) => new { pm.Input, ConsumerStackId = n.StackId, pm.ConsumerModule.Name })
            .Where(x => x.ConsumerStackId != newNamespaceStack)
            .Select(x => new { x.Input.Id, x.Input.Name, ConsumerModuleId = x.Input.ModuleId, ConsumerModuleName = x.Name, x.ConsumerStackId, ReferenceType = "ModuleParamFromOutputSet" })
            .ToListAsync();

        // Combine all incoming cross-stack references
        var allIncomingCrossStackReferences = incomingParamFromOutputReferences
            .Concat(incomingEnvVarFromOutputReferences.Cast<dynamic>())
            .Concat(incomingParamFromOutputSetReferences)
            .ToList();

        if (allCrossStackReferences.Any() || allCrossNamespaceReferences.Any() || allIncomingCrossStackReferences.Any())
        {
            var errorParts = new List<string>();

            // Get stack and namespace information for the target
            var newNamespace = await DbContext.Namespaces
                .Include(n => n.Stack)
                .FirstOrDefaultAsync(n => n.Id == newNamespaceId);

            if (allCrossStackReferences.Any() || allCrossNamespaceReferences.Any())
            {
                // Build detailed outgoing reference information
                var outgoingDetails = await BuildOutgoingReferenceDetails(allCrossStackReferences, allCrossNamespaceReferences);
                errorParts.Add(
                    $"This Module (ID {existingModule.Id}, Name: {existingModule.Name}) has references to Outputs or OutputSets from Modules that are outside the Stack (ID {newNamespace.StackId}, Name: {newNamespace.Stack.Name}) that the \"Update\" step is going to move the Module to:\n{outgoingDetails}");
            }

            if (allIncomingCrossStackReferences.Any())
            {
                // Build detailed incoming reference information
                var incomingDetails = await BuildIncomingReferenceDetails(allIncomingCrossStackReferences, existingModule, newNamespace);
                errorParts.Add(
                    $"The Outputs or OutputSets of this Module (ID {existingModule.Id}, Name: {existingModule.Name}) are referenced by Modules that are outside the Stack (ID {newNamespace.StackId}, Name: {newNamespace.Stack.Name}) that the \"Update\" step is going to move the Module to:\n{incomingDetails}");
            }

            throw new InvalidStackReferenceException(
                $"Cannot move module '{existingModule.Name}' to a different namespace because:\n\n{string.Join("\n\n", errorParts)}");
        }
    }

    private async Task<string> BuildOutgoingReferenceDetails(
        IEnumerable<dynamic> crossStackReferences,
        IEnumerable<dynamic> crossNamespaceReferences)
    {
        var details = new List<string>();

        // Get all referenced module IDs (only from cross-stack references)
        var allReferencedModuleIds = crossStackReferences
            .Select(r => r.OutputModuleId)
            .Cast<Guid>()
            .Distinct()
            .ToList();

        // Get module, namespace, and stack information for referenced modules
        var referencedModuleInfo = await DbContext.Modules
            .Where(m => allReferencedModuleIds.Contains(m.Id))
            .Join(DbContext.Namespaces,
                m => m.NamespaceId,
                n => n.Id,
                (m, n) => new { Module = m, Namespace = n })
            .Join(DbContext.Stacks,
                mn => mn.Namespace.StackId,
                s => s.Id,
                (mn, s) => new
                {
                    ModuleId = mn.Module.Id,
                    ModuleName = mn.Module.Name,
                    NamespaceName = mn.Namespace.Name,
                    StackId = s.Id,
                    StackName = s.Name
                })
            .ToDictionaryAsync(x => x.ModuleId);

        // Process cross-stack references
        foreach (var reference in crossStackReferences)
        {
            var refModuleId = (Guid)reference.OutputModuleId;
            var refInfo = referencedModuleInfo[refModuleId];
            var referenceType = GetReferenceType(reference);

            details.Add(
                $"- {referenceType} (ID {GetReferenceId(reference)}, Name: {reference.Name}), referencing OutputModuleId {refModuleId} (Name: {refInfo.ModuleName}, NamespaceName: {refInfo.NamespaceName}) in Stack (ID {refInfo.StackId}, Name: {refInfo.StackName})");
        }

        // Process cross-namespace references
        var namespaceIds = crossNamespaceReferences
            .Select(r => r.NamespaceId)
            .Cast<Guid>()
            .Distinct()
            .ToList();

        var namespaceInfo = await DbContext.Namespaces
            .Include(n => n.Stack)
            .Where(n => namespaceIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id);

        foreach (var reference in crossNamespaceReferences)
        {
            var refNamespaceId = (Guid)reference.NamespaceId;
            var namespaceDetails = namespaceInfo[refNamespaceId];
            var referenceType = GetReferenceType(reference);

            details.Add(
                $"- {referenceType} (ID {GetReferenceId(reference)}, Name: {reference.Name}), referencing NamespaceId {refNamespaceId} (Name: {namespaceDetails.Name}) in Stack (ID {namespaceDetails.StackId}, Name: {namespaceDetails.Stack.Name})");
        }

        return string.Join("\n", details);
    }

    private async Task<string> BuildIncomingReferenceDetails(
        IEnumerable<dynamic> incomingReferences,
        Module existingModule,
        Namespace newNamespace)
    {
        var details = new List<string>();

        // Get all consumer module IDs
        var consumerModuleIds = incomingReferences
            .Select(r => r.ConsumerModuleName)
            .Distinct()
            .ToList();

        // Get consumer module information
        var consumerModuleInfo = await DbContext.Modules
            .Where(m => consumerModuleIds.Contains(m.Name))
            .Join(DbContext.Namespaces,
                m => m.NamespaceId,
                n => n.Id,
                (m, n) => new { Module = m, Namespace = n })
            .Join(DbContext.Stacks,
                mn => mn.Namespace.StackId,
                s => s.Id,
                (mn, s) => new
                {
                    ModuleName = mn.Module.Name,
                    NamespaceName = mn.Namespace.Name,
                    StackId = s.Id,
                    StackName = s.Name
                })
            .ToDictionaryAsync(x => x.ModuleName);

        // Build details for each reference
        foreach (var reference in incomingReferences)
        {
            var consumerName = reference.ConsumerModuleName;
            var consumerInfo = consumerModuleInfo[consumerName];
            var referenceType = reference.ReferenceType;
            var referenceId = reference.Id;

            details.Add(
                $"- \"{referenceType}\" (ID \"{referenceId}\", Name: \"{reference.Name}\") from Module (ID \"{reference.ConsumerModuleId}\", Name: \"{consumerName}\", NamespaceName: \"{consumerInfo.NamespaceName}\") in Stack (ID \"{consumerInfo.StackId}\", Name: \"{consumerInfo.StackName}\")");
        }

        return string.Join("\n", details);
    }

    private string GetReferenceType(dynamic reference)
    {
        var type = reference.GetType();
        if (type.Name.Contains("ModuleParamFromOutput") && !type.Name.Contains("Set"))
            return "ModuleParamFromOutput";
        if (type.Name.Contains("ModuleParamFromOutputSet"))
            return "ModuleParamFromOutputSet";
        if (type.Name.Contains("ModuleEnvVarFromOutput"))
            return "ModuleEnvVarFromOutput";
        if (type.Name.Contains("ModuleParamFromNamespace"))
            return "ModuleParamFromNamespace";
        if (type.Name.Contains("ModuleEnvVarFromNamespace"))
            return "ModuleEnvVarFromNamespace";

        return "Unknown";
    }

    private Guid GetReferenceId(dynamic reference)
    {
        // For the anonymous types created in the validation, we don't have an Id
        // Try OutputModuleId first (for cross-stack references), then NamespaceId (for cross-namespace references)
        try
        {
            return (Guid)reference.OutputModuleId;
        }
        catch
        {
            return (Guid)reference.NamespaceId;
        }
    }
}