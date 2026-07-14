// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Namespaces;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceRepositorySettings> options)
{
    public NamespaceRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceRepository : GenericRepository<Namespace, NamespaceReadDto, NamespaceCreatedEvent, NamespaceUpdatedEvent, NamespaceDeletedEvent, NamespaceRepositorySettings>
{
    public NamespaceRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<NamespaceRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }
    
    protected override async Task SetServicePrincipalOwner(Guid id, Guid organizationId, Guid servicePrincipalId)
    {
        DbContext.ServicePrincipalNamespaceRoleAssignments.Add(new ServicePrincipalNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = id,
            ServicePrincipalId = servicePrincipalId,
            RoleName = NamespaceRole.Owner
        });
    }

    protected override async Task SetUserOwner(Guid id, Guid organizationId, Guid userId)
    {
        DbContext.UserNamespaceRoleAssignments.Add(new UserNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = id,
            UserId = userId,
            RoleName = NamespaceRole.Owner
        });
    }

    protected override List<object> AdditionalCreateMessages(Namespace entity)
    {
        var messages = new List<object>();
        messages.Add(new NamespaceModifiedEvent { Id = entity.Id, OrganizationId = entity.OrganizationId });
        return messages;
    }

    protected override List<object> AdditionalUpdateMessages(Namespace entity)
    {
        var messages = new List<object>();
        messages.Add(new NamespaceModifiedEvent { Id = entity.Id, OrganizationId = entity.OrganizationId });
        return messages;
    }


    protected override NamespaceReadDto MapToDto(Namespace entity)
    {
        return NamespaceMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(Namespace entity)
    {
        var currentCount = await DbContext.Namespaces
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespaceQuota), currentCount);
    }

    public async Task<Namespace> Get(Guid stackId, string name, Guid organizationId)
    {
        var @namespace = await DbContext.Namespaces
            .SingleOrDefaultAsync(i => i.Name == name && i.StackId == stackId && i.OrganizationId == organizationId);

        if (@namespace == null) throw new EntityNotFoundException($"Namespace with name {name} not found.");

        return @namespace;
    }

    public async Task<Namespace> Get(string stackName, string name, Guid organizationId)
    {
        var @namespace = await DbContext.Namespaces
            .Include(x => x.Stack)
            .SingleOrDefaultAsync(i => i.Name == name && i.Stack.Name == stackName && i.OrganizationId == organizationId);

        if (@namespace == null) throw new EntityNotFoundException($"Namespace with name {name} in stack with name {stackName} not found.");

        return @namespace;
    }


    public override async Task<Namespace> ExecuteCreate(Namespace entity)
    {
        NameValidator.EnsureValid(entity.Name, "Namespace");

        if (entity.DefaultEngine == StateManagementEngine.Pulumi)
            await ValidatePulumiFeatureEnabled(entity.OrganizationId);

        return await base.ExecuteCreate(entity);
    }

    public override async Task<Namespace> ExecuteUpdate(Namespace entity)
    {
        NameValidator.EnsureValid(entity.Name, "Namespace");

        // First get the existing namespace to check for namespace changes
        var existingNamespace = await Get(entity.Id, entity.OrganizationId);

        if (entity.DefaultEngine == StateManagementEngine.Pulumi && existingNamespace.DefaultEngine != StateManagementEngine.Pulumi)
            await ValidatePulumiFeatureEnabled(entity.OrganizationId);

        // Check if the namespace is changing
        if (existingNamespace.StackId != entity.StackId) await ValidateStackChange(existingNamespace, entity.StackId);

        // Proceed with the base update
        var updated = await base.ExecuteUpdate(entity);

        return updated;
    }

    public override async Task ExecuteDelete(Guid id, Guid organizationId)
    {
        // Delete all ModuleInputFromSecret records for modules in this namespace
        // This must be done first to avoid FK constraint violations when modules are deleted
        await DbContext.ModuleParamFromSecrets
            .Where(mi => mi.Module.NamespaceId == id)
            .ExecuteDeleteAsync();

        await DbContext.ModuleEnvVarFromSecrets
            .Where(mi => mi.Module.NamespaceId == id)
            .ExecuteDeleteAsync();

        // Delete all module-scoped secrets for modules in this namespace
        // This prevents FK_Secrets_Modules_ModuleId constraint violations
        await DbContext.ModuleSecrets
            .Where(s => s.Module.NamespaceId == id)
            .ExecuteDeleteAsync();

        // Delete all NamespaceInputFromSecret records for this namespace
        // This must be done before deleting the secrets to avoid FK constraint violations
        await DbContext.NamespaceParamFromSecrets
            .Where(mi => mi.NamespaceId == id)
            .ExecuteDeleteAsync();

        await DbContext.NamespaceEnvVarFromSecrets
            .Where(mi => mi.NamespaceId == id)
            .ExecuteDeleteAsync();

        // Now we can safely delete the namespace-scoped secrets
        await DbContext.NamespaceSecrets
            .Where(s => s.NamespaceId == id)
            .ExecuteDeleteAsync();

        // Now proceed with normal namespace deletion (which will cascade to modules)
        await base.ExecuteDelete(id, organizationId);
    }


    private async Task ValidatePulumiFeatureEnabled(Guid organizationId)
    {
        var accepted = await DbContext.PreviewFeatureAcceptances
            .AnyAsync(p => p.OrganizationId == organizationId && p.PreviewFeature == PreviewFeature.Pulumi);

        if (!accepted)
            throw new PreviewFeatureNotEnabledException(
                "Cannot set Default Engine to Pulumi because the organization has not opted into the Pulumi preview feature.");
    }

    private async Task ValidateStackChange(Namespace existingNamespace, Guid newStackId)
    {
        // Get all modules in the namespace
        var moduleIds = await DbContext.Modules
            .Where(m => m.NamespaceId == existingNamespace.Id && m.OrganizationId == existingNamespace.OrganizationId)
            .Select(m => m.Id)
            .ToListAsync();

        if (!moduleIds.Any())
            return; // No modules to validate

        // Check for cross-stack references in ModuleParamFromOutput entities
        var paramFromOutputCrossStackReferences = await DbContext.ModuleParamFromOutputs
            .Where(p => moduleIds.Contains(p.ModuleId))
            .Join(DbContext.Modules,
                p => p.OutputModuleId,
                m => m.Id,
                (p, m) => new { Input = p, OutputModule = m })
            .Where(x => x.OutputModule.OrganizationId == existingNamespace.OrganizationId)
            .Join(DbContext.Namespaces,
                pm => pm.OutputModule.NamespaceId,
                n => n.Id,
                (pm, n) => new { pm.Input, OutputStackId = n.StackId })
            .Where(x => x.OutputStackId != newStackId)
            .Select(x => new { x.Input.Name, x.Input.ModuleId, x.Input.OutputModuleId, x.OutputStackId })
            .ToListAsync();

        // Check for cross-stack references in ModuleEnvVarFromOutput entities
        var envVarFromOutputCrossStackReferences = await DbContext.ModuleEnvVarFromOutputs
            .Where(e => moduleIds.Contains(e.ModuleId))
            .Join(DbContext.Modules,
                e => e.OutputModuleId,
                m => m.Id,
                (e, m) => new { Input = e, OutputModule = m })
            .Where(x => x.OutputModule.OrganizationId == existingNamespace.OrganizationId)
            .Join(DbContext.Namespaces,
                em => em.OutputModule.NamespaceId,
                n => n.Id,
                (em, n) => new { em.Input, OutputStackId = n.StackId })
            .Where(x => x.OutputStackId != newStackId)
            .Select(x => new { x.Input.Name, x.Input.ModuleId, x.Input.OutputModuleId, x.OutputStackId })
            .ToListAsync();

        // Check for cross-stack references in ModuleParamFromOutputSet entities
        var paramFromOutputSetCrossStackReferences = await DbContext.ModuleParamFromOutputSets
            .Where(p => moduleIds.Contains(p.ModuleId))
            .Join(DbContext.Modules,
                p => p.OutputModuleId,
                m => m.Id,
                (p, m) => new { Input = p, OutputModule = m })
            .Where(x => x.OutputModule.OrganizationId == existingNamespace.OrganizationId)
            .Join(DbContext.Namespaces,
                pm => pm.OutputModule.NamespaceId,
                n => n.Id,
                (pm, n) => new { pm.Input, OutputStackId = n.StackId })
            .Where(x => x.OutputStackId != newStackId)
            .Select(x => new { x.Input.Name, x.Input.ModuleId, x.Input.OutputModuleId, x.OutputStackId })
            .ToListAsync();

        // Combine all cross-stack references
        var allCrossStackReferences = paramFromOutputCrossStackReferences
            .Concat(envVarFromOutputCrossStackReferences)
            .Concat(paramFromOutputSetCrossStackReferences)
            .Distinct()
            .ToList();

        // Check for incoming references - modules in different stacks that reference modules in this namespace as OutputModuleId
        var incomingParamFromOutputReferences = await DbContext.ModuleParamFromOutputs
            .Where(p => moduleIds.Contains(p.OutputModuleId))
            .Join(DbContext.Modules,
                p => p.ModuleId,
                m => m.Id,
                (p, m) => new { Input = p, ConsumerModule = m })
            .Where(x => x.ConsumerModule.OrganizationId == existingNamespace.OrganizationId)
            .Join(DbContext.Namespaces,
                pm => pm.ConsumerModule.NamespaceId,
                n => n.Id,
                (pm, n) => new { pm.Input, ConsumerStackId = n.StackId, pm.ConsumerModule.Name })
            .Where(x => x.ConsumerStackId != newStackId)
            .Select(x => new
            {
                x.Input.Id, x.Input.Name, ConsumerModuleId = x.Input.ModuleId, ConsumerModuleName = x.Name, x.Input.OutputModuleId, x.ConsumerStackId, ReferenceType = "ModuleParamFromOutput"
            })
            .ToListAsync();

        var incomingEnvVarFromOutputReferences = await DbContext.ModuleEnvVarFromOutputs
            .Where(e => moduleIds.Contains(e.OutputModuleId))
            .Join(DbContext.Modules,
                e => e.ModuleId,
                m => m.Id,
                (e, m) => new { Input = e, ConsumerModule = m })
            .Where(x => x.ConsumerModule.OrganizationId == existingNamespace.OrganizationId)
            .Join(DbContext.Namespaces,
                em => em.ConsumerModule.NamespaceId,
                n => n.Id,
                (em, n) => new { em.Input, ConsumerStackId = n.StackId, em.ConsumerModule.Name })
            .Where(x => x.ConsumerStackId != newStackId)
            .Select(x => new
            {
                x.Input.Id, x.Input.Name, ConsumerModuleId = x.Input.ModuleId, ConsumerModuleName = x.Name, x.Input.OutputModuleId, x.ConsumerStackId, ReferenceType = "ModuleEnvVarFromOutput"
            })
            .ToListAsync();

        var incomingParamFromOutputSetReferences = await DbContext.ModuleParamFromOutputSets
            .Where(p => moduleIds.Contains(p.OutputModuleId))
            .Join(DbContext.Modules,
                p => p.ModuleId,
                m => m.Id,
                (p, m) => new { Input = p, ConsumerModule = m })
            .Where(x => x.ConsumerModule.OrganizationId == existingNamespace.OrganizationId)
            .Join(DbContext.Namespaces,
                pm => pm.ConsumerModule.NamespaceId,
                n => n.Id,
                (pm, n) => new { pm.Input, ConsumerStackId = n.StackId, pm.ConsumerModule.Name })
            .Where(x => x.ConsumerStackId != newStackId)
            .Select(x => new
            {
                x.Input.Id, x.Input.Name, ConsumerModuleId = x.Input.ModuleId, ConsumerModuleName = x.Name, x.Input.OutputModuleId, x.ConsumerStackId, ReferenceType = "ModuleParamFromOutputSet"
            })
            .ToListAsync();

        // Combine all incoming cross-stack references
        var allIncomingCrossStackReferences = incomingParamFromOutputReferences
            .Concat(incomingEnvVarFromOutputReferences.Cast<dynamic>())
            .Concat(incomingParamFromOutputSetReferences)
            .Distinct()
            .ToList();

        if (allCrossStackReferences.Any() || allIncomingCrossStackReferences.Any())
        {
            var errorParts = new List<string>();

            // Get target stack information
            var newStack = await DbContext.Stacks
                .FirstOrDefaultAsync(s => s.Id == newStackId && s.OrganizationId == existingNamespace.OrganizationId)
                ?? throw new InvalidOperationException($"Target stack with ID {newStackId} not found in organization {existingNamespace.OrganizationId}.");

            if (allCrossStackReferences.Any())
            {
                // Build detailed outgoing reference information
                var outgoingDetails = await BuildNamespaceOutgoingReferenceDetails(allCrossStackReferences, existingNamespace, newStack);
                errorParts.Add(
                    $"Modules in this Namespace (ID {existingNamespace.Id}, Name: {existingNamespace.Name}) have references to Outputs or OutputSets from Modules that are outside the Stack (ID {newStackId}, Name: {newStack.Name}) that the \"Update\" step is going to move the Namespace to:\n{outgoingDetails}");
            }

            if (allIncomingCrossStackReferences.Any())
            {
                // Build detailed incoming reference information
                var incomingDetails = await BuildNamespaceIncomingReferenceDetails(allIncomingCrossStackReferences, existingNamespace, newStack);
                errorParts.Add(
                    $"The Outputs or OutputSets of Modules in this Namespace (ID {existingNamespace.Id}, Name: {existingNamespace.Name}) are referenced by Modules that are outside the Stack (ID {newStackId}, Name: {newStack.Name}) that the \"Update\" step is going to move the Namespace to:\n{incomingDetails}");
            }

            throw new InvalidStackReferenceException(
                $"Cannot move namespace '{existingNamespace.Name}' to a different stack because:\n\n{string.Join("\n\n", errorParts)}");
        }
    }

    private async Task<string> BuildNamespaceOutgoingReferenceDetails(
        IEnumerable<dynamic> crossStackReferences,
        Namespace existingNamespace,
        Stack newStack)
    {
        var details = new List<string>();

        // Get all modules in the namespace
        var namespaceModules = await DbContext.Modules
            .Where(m => m.NamespaceId == existingNamespace.Id && m.OrganizationId == existingNamespace.OrganizationId)
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        // Get all referenced module IDs
        var allReferencedModuleIds = crossStackReferences
            .Select(r => r.OutputModuleId)
            .Cast<Guid>()
            .Distinct()
            .ToList();

        // Get module, namespace, and stack information for referenced modules
        var referencedModuleInfo = await DbContext.Modules
            .Where(m => allReferencedModuleIds.Contains(m.Id) && m.OrganizationId == existingNamespace.OrganizationId)
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

        // Group by module in the namespace
        var groupedByModule = crossStackReferences.GroupBy(r => r.ModuleId);

        foreach (var moduleGroup in groupedByModule)
        {
            var moduleId = (Guid)moduleGroup.Key;
            var moduleName = namespaceModules[moduleId];

            details.Add($"  Module (ID {moduleId}, Name: {moduleName}) has references to:");

            foreach (var reference in moduleGroup)
            {
                var refModuleId = (Guid)reference.OutputModuleId;
                var refInfo = referencedModuleInfo[refModuleId];
                var referenceType = GetReferenceType(reference);

                details.Add(
                    $"    - {referenceType} (ID {GetReferenceId(reference)}, Name: {reference.Name}), referencing OutputModuleId {refModuleId} (Name: {refInfo.ModuleName}, NamespaceName: {refInfo.NamespaceName}) in Stack (ID {refInfo.StackId}, Name: {refInfo.StackName})");
            }
        }

        return string.Join("\n", details);
    }

    private async Task<string> BuildNamespaceIncomingReferenceDetails(
        IEnumerable<dynamic> incomingReferences,
        Namespace existingNamespace,
        Stack newStack)
    {
        var details = new List<string>();

        // Get all modules in the namespace
        var namespaceModules = await DbContext.Modules
            .Where(m => m.NamespaceId == existingNamespace.Id && m.OrganizationId == existingNamespace.OrganizationId)
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        // Get all consumer module IDs
        var consumerModuleIds = incomingReferences
            .Select(r => r.ConsumerModuleName)
            .Distinct()
            .ToList();

        // Get consumer module information
        var consumerModuleInfo = await DbContext.Modules
            .Where(m => consumerModuleIds.Contains(m.Name) && m.OrganizationId == existingNamespace.OrganizationId)
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
            var outputModuleId = (Guid)reference.OutputModuleId;
            var outputModuleName = namespaceModules[outputModuleId];
            var consumerName = reference.ConsumerModuleName;
            var consumerInfo = consumerModuleInfo[consumerName];
            var referenceType = reference.ReferenceType;
            var referenceId = reference.Id;

            details.Add(
                $"- \"{referenceType}\" (ID \"{referenceId}\", Name: \"{reference.Name}\") from Module (ID \"{reference.ConsumerModuleId}\", Name: \"{consumerName}\", NamespaceName: \"{consumerInfo.NamespaceName}\") in Stack (ID \"{consumerInfo.StackId}\", Name: \"{consumerInfo.StackName}\") referencing Module (ID \"{outputModuleId}\", Name: \"{outputModuleName}\")");
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

        return "Unknown";
    }

    private Guid GetReferenceId(dynamic reference)
    {
        // For the anonymous types created in the validation, we don't have an Id
        // Return the OutputModuleId instead, which is what we need for reference tracking
        return (Guid)reference.OutputModuleId;
    }

    protected override Func<IQueryable<Namespace>, IQueryable<Namespace>> ByParentIdQueryModifier(Guid stackId)
    {
        return query => query.Where(n => n.StackId == stackId);
    }
}