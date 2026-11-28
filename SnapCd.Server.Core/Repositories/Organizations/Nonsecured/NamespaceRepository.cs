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
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using System.Linq.Expressions;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;
using NameValidator = SnapCd.Server.Core.Services.Utils.NameValidator;

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

public class NamespaceRepository : GenericRepository<Namespace, NamespaceReadDto, NamespaceMetadata, NamespaceCreatedEvent, NamespaceUpdatedEvent, NamespaceDeletedEvent, NamespaceRepositorySettings>
{
    /// <summary>How many Namespaces may sit above one. The ancestry closure holds no row beyond it.</summary>
    public const int MaxDepth = 20;

    protected override Expression<Func<Namespace, NamespaceMetadata>> MetadataProjection =>
        e => new NamespaceMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            Name = e.Name,
            StackId = e.StackId
        };
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



    /// <summary>
    /// The scope with the names above it, which metadata deliberately does not carry: metadata
    /// is a join-free read, and a picker needs enough to tell one "default" from another.
    /// </summary>
    /// <summary>One scope with the names above it, for a preselected picker.</summary>
    public async Task<QualifiedNamespace> GetQualifiedNamespace(Guid id, Guid organizationId)
        => (await ListQualifiedNamespace(organizationId, queryModifier: q => q.Where(e => e.Id == id))).First();

    public Task<List<QualifiedNamespace>> ListQualifiedNamespace(
        Guid organizationId,
        IQueryable<Namespace>? query = null,
        Func<IQueryable<Namespace>, IQueryable<Namespace>>? queryModifier = null,
        Func<IQueryable<QualifiedNamespace>, IOrderedQueryable<QualifiedNamespace>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
        => List(
            organizationId,
            q => (queryModifier is null ? q : queryModifier(q)).Select(e => new QualifiedNamespace
            {
                Id = e.Id,
                OrganizationId = e.OrganizationId,
                Name = e.Name,
                StackName = e.Stack.Name
            }),
            query,
            orderBy,
            pageNumber,
            pageSize);

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
            .SingleOrDefaultAsync(i => i.Name == name && i.StackId == stackId && i.Id != i.StackId && i.OrganizationId == organizationId);

        if (@namespace == null) throw new EntityNotFoundException($"Namespace with name {name} not found.");

        return @namespace;
    }

    public async Task<Namespace> Get(string stackName, string name, Guid organizationId)
    {
        var @namespace = await DbContext.Namespaces
            .Include(x => x.Stack)
            .SingleOrDefaultAsync(i => i.Name == name && i.Stack.Name == stackName && i.Id != i.StackId && i.OrganizationId == organizationId);

        if (@namespace == null) throw new EntityNotFoundException($"Namespace with name {name} in stack with name {stackName} not found.");

        return @namespace;
    }


    public async Task<Namespace> Get(int number, Guid organizationId)
    {
        var @namespace = await DbContext.Namespaces
            .SingleOrDefaultAsync(i => i.Number == number && i.OrganizationId == organizationId);

        if (@namespace == null) throw new EntityNotFoundException($"Namespace with number {number} not found.");

        return @namespace;
    }


    public override async Task<Namespace> ExecuteCreate(Namespace entity)
    {
        NameValidator.EnsureValid(entity.Name, "Namespace");

        if (entity.ParentNamespaceId is { } parentId)
            await EnsureCanSitUnder(parentId, entity.OrganizationId, levelsBelow: 0);

        return await base.ExecuteCreate(entity);
    }

    public override async Task<Namespace> ExecuteUpdate(Namespace entity)
    {
        NameValidator.EnsureValid(entity.Name, "Namespace");

        var existingNamespace = OriginalOf(await Get(entity.Id, entity.OrganizationId));

        if (existingNamespace.ParentNamespaceId != entity.ParentNamespaceId && entity.ParentNamespaceId is { } newParentId)
            await EnsureCanMoveUnder(entity, newParentId);

        // Proceed with the base update
        var updated = await base.ExecuteUpdate(entity);

        // Publish approval threshold event if changed. Modules that don't override the
        // threshold inherit this default, so lowering it can make an already-waiting job
        // approved - NamespaceApprovalThresholdModifiedCompetingConsumer fans this out to
        // every module in the namespace, which re-evaluates their waiting jobs. Mirrors
        // ModuleRepository.ExecuteUpdate.
        if (updated.DefaultApplyApprovalThreshold != existingNamespace.DefaultApplyApprovalThreshold ||
            updated.DefaultDestroyApprovalThreshold != existingNamespace.DefaultDestroyApprovalThreshold ||
            updated.DefaultStateMigrationApprovalThreshold != existingNamespace.DefaultStateMigrationApprovalThreshold)
            // No time to live: a dropped message leaves waiting jobs on a threshold that no longer applies.
            await EnqueueOrPublish(() => Bus.Publish(new NamespaceApprovalThresholdModifiedEvent
            {
                NamespaceId = updated.Id
            }));

        return updated;
    }

    public override async Task ExecuteDelete(Guid id, Guid organizationId)
    {
        // A Namespace holding children is deleted from the bottom up, so that removing one does
        // not silently take an arbitrarily deep tree of Namespaces and their Modules with it.
        var childNames = await DbContext.Namespaces
            .Where(n => n.ParentNamespaceId == id && n.OrganizationId == organizationId)
            .Select(n => n.Name)
            .ToListAsync();

        if (childNames.Count > 0)
            throw new InvalidNamespaceRefereceException(
                $"Cannot delete this Namespace because it still contains {childNames.Count} " +
                $"Namespace(s): {string.Join(", ", childNames)}. Delete or move them first.");

        // The Modules go by cascade rather than through ModuleRepository, so what a Module cannot
        // take with it is cleared here too.
        await DbContext.ClearRowsBlockingModuleDelete(m => m.NamespaceId == id, organizationId);

        await DbContext.ClearRowsBlockingNamespaceDelete(n => n.Id == id, organizationId);

        await base.ExecuteDelete(id, organizationId);
    }


    private async Task EnsureCanMoveUnder(Namespace moved, Guid newParentId)
    {
        var below = await DbContext.NamespaceAncestors
            .Where(a => a.AncestorNamespaceId == moved.Id && a.OrganizationId == moved.OrganizationId)
            .Select(a => new { a.NamespaceId, a.Depth })
            .ToListAsync();

        if (newParentId == moved.Id || below.Any(a => a.NamespaceId == newParentId))
            throw new InvalidNamespaceRefereceException(
                $"Cannot move Namespace '{moved.Name}' under Namespace with ID {newParentId}, because that Namespace is '{moved.Name}' itself or is inside it.");

        await EnsureCanSitUnder(newParentId, moved.OrganizationId, levelsBelow: below.Select(a => a.Depth).DefaultIfEmpty(0).Max());
    }

    /// <param name="levelsBelow">How many levels of Namespaces come along underneath the one being placed.</param>
    private async Task EnsureCanSitUnder(Guid parentId, Guid organizationId, int levelsBelow)
    {
        var parentDepths = await DbContext.NamespaceAncestors
            .Where(a => a.NamespaceId == parentId && a.OrganizationId == organizationId)
            .Select(a => a.Depth)
            .ToListAsync();

        if (parentDepths.Count == 0)
            throw new InvalidNamespaceRefereceException($"Parent Namespace with ID {parentId} not found.");

        var deepest = parentDepths.Max() + 1 + levelsBelow;

        if (deepest > MaxDepth)
            throw new InvalidNamespaceRefereceException(
                $"Namespaces can be nested at most {MaxDepth} levels deep, and this would place one {deepest} levels deep.");
    }

    protected override Func<IQueryable<Namespace>, IQueryable<Namespace>> ByParentIdQueryModifier(Guid stackId)
    {
        return query => query.Where(n => n.StackId == stackId);
    }
}