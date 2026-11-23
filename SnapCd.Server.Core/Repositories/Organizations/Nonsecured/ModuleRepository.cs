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
using SnapCd.Contracts.Dto.Modules;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using System.Linq.Expressions;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;
using NameValidator = SnapCd.Server.Core.Services.Utils.NameValidator;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

/// <summary>
/// One (state, running) bucket from the ModuleState table with its module count.
/// </summary>
public class ModuleStateCount
{
    public string? StateHeadline { get; set; }
    public bool IsRunning { get; set; }
    public int Count { get; set; }
}

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

public class ModuleRepository : GenericNamespaceChildRepository<Module, ModuleReadDto, ModuleMetadata, ModuleCreatedEvent, ModuleUpdatedEvent, ModuleDeletedEvent, ModuleRepositorySettings>
{

    protected override Expression<Func<Module, ModuleMetadata>> MetadataProjection =>
        e => new ModuleMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            Name = e.Name,
            NamespaceId = e.NamespaceId,
            RunnerId = e.RunnerId
        };
    public ModuleRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<ModuleRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }


    /// <summary>
    /// The scope with the names above it, which metadata deliberately does not carry: metadata
    /// is a join-free read, and a picker needs enough to tell one "app" from another.
    /// </summary>
    /// <summary>One scope with the names above it, for a preselected picker.</summary>
    public async Task<QualifiedModule> GetQualifiedModule(Guid id, Guid organizationId)
        => (await ListQualifiedModule(organizationId, queryModifier: q => q.Where(e => e.Id == id))).First();

    public Task<List<QualifiedModule>> ListQualifiedModule(
        Guid organizationId,
        IQueryable<Module>? query = null,
        Func<IQueryable<Module>, IQueryable<Module>>? queryModifier = null,
        Func<IQueryable<QualifiedModule>, IOrderedQueryable<QualifiedModule>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
        => List(
            organizationId,
            q => (queryModifier is null ? q : queryModifier(q)).Select(e => new QualifiedModule
            {
                Id = e.Id,
                OrganizationId = e.OrganizationId,
                Name = e.Name,
                NamespaceName = e.Namespace.Name,
                StackName = e.Namespace.Stack.Name
            }),
            query,
            orderBy,
            pageNumber,
            pageSize);

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

    /// <summary>
    /// Aggregated per-state module counts for the organization, read from the trigger-maintained
    /// ModuleState table — no per-module job subqueries. Joined against Modules because
    /// ModuleState has no FK and may hold orphaned rows for deleted modules. Every module carries
    /// a row from creation, with a null headline until it has run something.
    /// </summary>
    public async Task<List<ModuleStateCount>> GetModuleStateCounts(Guid organizationId)
    {
        return await DbContext.Database
            .SqlQuery<ModuleStateCount>($@"
                SELECT ms.LatestActualStateHeadline AS StateHeadline, ms.IsRunning, COUNT(*) AS [Count]
                FROM ModuleState ms
                INNER JOIN Modules m ON m.Id = ms.ModuleId
                WHERE ms.OrganizationId = {organizationId}
                GROUP BY ms.LatestActualStateHeadline, ms.IsRunning")
            .ToListAsync();
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
        NameValidator.EnsureValid(entity.Name, "Module");

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

        return await base.ExecuteCreate(entity);
    }

    public override async Task<Module> ExecuteUpdate(Module entity)
    {
        NameValidator.EnsureValid(entity.Name, "Module");

        var existingModule = OriginalOf(await Get(entity.Id, entity.OrganizationId));

        if (existingModule.NamespaceId != entity.NamespaceId)
            await EnsureNamespaceExists(entity.NamespaceId, entity.OrganizationId);

        // Proceed with the base update
        var updated = await base.ExecuteUpdate(entity);

        // Publish approval threshold event if changed
        if (updated.ApplyApprovalThreshold != existingModule.ApplyApprovalThreshold ||
            updated.DestroyApprovalThreshold != existingModule.DestroyApprovalThreshold ||
            updated.StateMigrationApprovalThreshold != existingModule.StateMigrationApprovalThreshold)
            // No time to live: a dropped message leaves waiting jobs on a threshold that no longer applies.
            await EnqueueOrPublish(() => Bus.Publish(new ModuleApprovalThresholdModifiedEvent
            {
                ModuleId = updated.Id
            }));

        return updated;
    }

    public override async Task ExecuteDelete(Guid id, Guid organizationId)
    {
        await DbContext.ClearRowsBlockingModuleDelete(m => m.Id == id, organizationId);

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


    private async Task EnsureNamespaceExists(Guid namespaceId, Guid organizationId)
    {
        if (!await DbContext.Namespaces.AnyAsync(n => n.Id == namespaceId && n.OrganizationId == organizationId))
            throw new InvalidNamespaceRefereceException($"Namespace with ID {namespaceId} not found.");
    }
}
