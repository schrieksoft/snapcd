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
using SnapCd.Contracts.Dto.Runners;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class RunnerRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<RunnerRepositorySettings> options)
{
    public RunnerRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerRepository : GenericOrganizationChildRepository<Runner, RunnerReadDto, RunnerCreatedEvent, RunnerUpdatedEvent, RunnerDeletedEvent, RunnerRepositorySettings>
{
    public RunnerRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override async Task SetServicePrincipalOwner(Guid id, Guid organizationId, Guid servicePrincipalId)
    {
        DbContext.ServicePrincipalRunnerRoleAssignments.Add(new ServicePrincipalRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RunnerId = id,
            ServicePrincipalId = servicePrincipalId,
            RoleName = RunnerRole.Owner
        });
    }

    protected override async Task SetUserOwner(Guid id, Guid organizationId, Guid userId)
    {
        DbContext.UserRunnerRoleAssignments.Add(new UserRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RunnerId = id,
            UserId = userId,
            RoleName = RunnerRole.Owner
        });
    }
    
    protected override RunnerReadDto MapToDto(Runner entity)
    {
        return RunnerMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(Runner entity)
    {
        var currentCount = await DbContext.Runners
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.RunnerQuota), currentCount);
    }

    public async Task<Runner> GetByName(string name, Guid organizationId)
    {
        var runner = await DbContext.Runners
            .Where(rp => rp.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Name == name);

        if (runner == null) throw new EntityNotFoundException($"Runner with name {name} not found.");

        return runner;
    }

    public async Task<List<Runner>> ListAssignedToModule(
        Guid moduleId,
        Guid organizationId,
        IQueryable<Runner>? query = null)
    {
        // Get module with navigation properties
        var module = await DbContext.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(x => new { x.Id, x.NamespaceId, StackId = x.Namespace.StackId })
            .FirstOrDefaultAsync();

        if (module == null)
            throw new EntityNotFoundException($"Module with ID {moduleId} not found.");

        query ??= DbContext.Set<Runner>();

        query = query.Where(rp =>
            rp.OrganizationId == organizationId &&
            (rp.RunnerModuleAssignments.Any(a => a.ModuleId == moduleId) ||
             rp.RunnerNamespaceAssignments.Any(a => a.NamespaceId == module.NamespaceId) ||
             rp.RunnerStackAssignments.Any(a => a.StackId == module.StackId) ||
             rp.IsAssignedToAllModules));

        return await query.ToListAsync();
    }

    public async Task<List<Runner>> ListAssignedToNamespace(
        Guid namespaceId,
        Guid organizationId,
        IQueryable<Runner>? query = null)
    {
        // Get namespace with navigation properties
        var ns = await DbContext.Namespaces
            .Where(n => n.Id == namespaceId && n.OrganizationId == organizationId)
            .Select(x => new { x.Id, x.StackId })
            .FirstOrDefaultAsync();

        if (ns == null)
            throw new EntityNotFoundException($"Namespace with ID {namespaceId} not found.");

        query ??= DbContext.Set<Runner>();

        query = query.Where(rp =>
            rp.OrganizationId == organizationId &&
            (rp.RunnerNamespaceAssignments.Any(a => a.NamespaceId == ns.Id) ||
             rp.RunnerStackAssignments.Any(a => a.StackId == ns.StackId) ||
             rp.IsAssignedToAllModules));

        return await query.ToListAsync();
    }

    public async Task<List<Runner>> ListAssignedToStack(
        Guid stackId,
        Guid organizationId,
        IQueryable<Runner>? query = null)
    {
        query ??= DbContext.Set<Runner>();

        query = query.Where(rp =>
            rp.OrganizationId == organizationId &&
            (rp.RunnerStackAssignments.Any(a => a.StackId == stackId) ||
             rp.IsAssignedToAllModules));

        return await query.ToListAsync();
    }
}