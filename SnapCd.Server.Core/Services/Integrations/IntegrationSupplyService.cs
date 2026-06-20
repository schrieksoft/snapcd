// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;

namespace SnapCd.Server.Core.Services.Integrations;

/// <summary>Manages an integration's supply assignments across the three scope tables. Reading requires
/// read permission on the integration; adding/removing requires update permission (managing supply is an
/// edit of the integration).</summary>
public sealed class IntegrationSupplyService(
    IntegrationStackSupplyRepositoryFactory stackRepoFactory,
    IntegrationNamespaceSupplyRepositoryFactory namespaceRepoFactory,
    IntegrationModuleSupplyRepositoryFactory moduleRepoFactory,
    IntegrationSecuredRepositoryFactory securedFactory,
    IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public async Task<List<IntegrationSupplyDto>> List(Guid integrationId, Guid organizationId)
    {
        using (var secured = securedFactory.Create())
        {
            if (!secured.CanRead(integrationId, organizationId))
                throw new PrincipalNotAuthorizedException($"Not permitted to read integration '{integrationId}'.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        var stacks = await db.IntegrationStackSupplies
            .Where(x => x.IntegrationId == integrationId && x.OrganizationId == organizationId)
            .Select(x => new IntegrationSupplyDto { Id = x.Id, Scope = IntegrationSupplyScope.Stack, ScopeId = x.StackId })
            .ToListAsync();
        var namespaces = await db.IntegrationNamespaceSupplies
            .Where(x => x.IntegrationId == integrationId && x.OrganizationId == organizationId)
            .Select(x => new IntegrationSupplyDto { Id = x.Id, Scope = IntegrationSupplyScope.Namespace, ScopeId = x.NamespaceId })
            .ToListAsync();
        var modules = await db.IntegrationModuleSupplies
            .Where(x => x.IntegrationId == integrationId && x.OrganizationId == organizationId)
            .Select(x => new IntegrationSupplyDto { Id = x.Id, Scope = IntegrationSupplyScope.Module, ScopeId = x.ModuleId })
            .ToListAsync();

        return [.. stacks, .. namespaces, .. modules];
    }

    public async Task<IntegrationSupplyDto> GetOne(Guid integrationId, Guid organizationId, IntegrationSupplyScope scope, Guid assignmentId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        IntegrationSupplyDto? dto = scope switch
        {
            IntegrationSupplyScope.Stack => await db.IntegrationStackSupplies
                .Where(x => x.Id == assignmentId && x.IntegrationId == integrationId && x.OrganizationId == organizationId)
                .Select(x => new IntegrationSupplyDto { Id = x.Id, Scope = IntegrationSupplyScope.Stack, ScopeId = x.StackId }).FirstOrDefaultAsync(),
            IntegrationSupplyScope.Namespace => await db.IntegrationNamespaceSupplies
                .Where(x => x.Id == assignmentId && x.IntegrationId == integrationId && x.OrganizationId == organizationId)
                .Select(x => new IntegrationSupplyDto { Id = x.Id, Scope = IntegrationSupplyScope.Namespace, ScopeId = x.NamespaceId }).FirstOrDefaultAsync(),
            IntegrationSupplyScope.Module => await db.IntegrationModuleSupplies
                .Where(x => x.Id == assignmentId && x.IntegrationId == integrationId && x.OrganizationId == organizationId)
                .Select(x => new IntegrationSupplyDto { Id = x.Id, Scope = IntegrationSupplyScope.Module, ScopeId = x.ModuleId }).FirstOrDefaultAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown assignment scope.")
        };
        return dto ?? throw new EntityNotFoundException($"Assignment '{assignmentId}' not found");
    }

    public async Task<Guid> Add(Guid integrationId, Guid organizationId, IntegrationSupplyCreateDto dto)
    {
        using (var secured = securedFactory.Create())
        {
            if (!secured.CanUpdate(integrationId, organizationId))
                throw new PrincipalNotAuthorizedException($"Not permitted to manage assignments on integration '{integrationId}'.");
        }

        var id = Guid.NewGuid();
        switch (dto.Scope)
        {
            case IntegrationSupplyScope.Stack:
                using (var repo = stackRepoFactory.Create())
                    await repo.Create(new IntegrationStackSupply { Id = id, OrganizationId = organizationId, IntegrationId = integrationId, StackId = dto.ScopeId });
                break;
            case IntegrationSupplyScope.Namespace:
                using (var repo = namespaceRepoFactory.Create())
                    await repo.Create(new IntegrationNamespaceSupply { Id = id, OrganizationId = organizationId, IntegrationId = integrationId, NamespaceId = dto.ScopeId });
                break;
            case IntegrationSupplyScope.Module:
                using (var repo = moduleRepoFactory.Create())
                    await repo.Create(new IntegrationModuleSupply { Id = id, OrganizationId = organizationId, IntegrationId = integrationId, ModuleId = dto.ScopeId });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dto), dto.Scope, "Unknown assignment scope.");
        }

        return id;
    }

    public async Task Remove(Guid organizationId, IntegrationSupplyScope scope, Guid assignmentId)
    {
        Guid? integrationId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            integrationId = scope switch
            {
                IntegrationSupplyScope.Stack => await db.IntegrationStackSupplies.Where(x => x.Id == assignmentId && x.OrganizationId == organizationId).Select(x => (Guid?)x.IntegrationId).FirstOrDefaultAsync(),
                IntegrationSupplyScope.Namespace => await db.IntegrationNamespaceSupplies.Where(x => x.Id == assignmentId && x.OrganizationId == organizationId).Select(x => (Guid?)x.IntegrationId).FirstOrDefaultAsync(),
                IntegrationSupplyScope.Module => await db.IntegrationModuleSupplies.Where(x => x.Id == assignmentId && x.OrganizationId == organizationId).Select(x => (Guid?)x.IntegrationId).FirstOrDefaultAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown assignment scope.")
            };
        }
        if (integrationId is null) throw new EntityNotFoundException($"Assignment '{assignmentId}' not found");

        using (var secured = securedFactory.Create())
        {
            if (!secured.CanUpdate(integrationId.Value, organizationId))
                throw new PrincipalNotAuthorizedException($"Not permitted to manage assignments on integration '{integrationId}'.");
        }

        switch (scope)
        {
            case IntegrationSupplyScope.Stack:
                using (var repo = stackRepoFactory.Create()) await repo.Delete(assignmentId, organizationId);
                break;
            case IntegrationSupplyScope.Namespace:
                using (var repo = namespaceRepoFactory.Create()) await repo.Delete(assignmentId, organizationId);
                break;
            case IntegrationSupplyScope.Module:
                using (var repo = moduleRepoFactory.Create()) await repo.Delete(assignmentId, organizationId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown assignment scope.");
        }
    }
}
