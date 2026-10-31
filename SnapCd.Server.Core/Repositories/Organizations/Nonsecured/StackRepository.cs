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
using SnapCd.Contracts.Dto.Stacks;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using Stack = SnapCd.Server.Core.Entities.Definition.Stack;
using System.Linq.Expressions;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;
using NameValidator = SnapCd.Server.Core.Services.Utils.NameValidator;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class StackRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<StackRepositorySettings> options, QuotaService quotaService)
{
    public StackRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StackRepository(dbContext, principalProvider, bus, options, quotaService);
    }
}

public class StackRepository : GenericOrganizationChildRepository<Stack, StackReadDto, StackMetadata, StackCreatedEvent, StackUpdatedEvent, StackDeletedEvent, StackRepositorySettings>
{

    protected override Expression<Func<Stack, StackMetadata>> MetadataProjection =>
        e => new StackMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            Name = e.Name
        };
    public StackRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<StackRepositorySettings> options,
        QuotaService? quotaService = null)
        : base(dbContext, principalProvider, bus, options, quotaService)
    {
    }

    
    protected override async Task SetServicePrincipalOwner(Guid id, Guid organizationId, Guid servicePrincipalId)
    {
        DbContext.ServicePrincipalStackRoleAssignments.Add(new ServicePrincipalStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StackId = id,
            ServicePrincipalId = servicePrincipalId,
            RoleName = StackRole.Owner
        });
    }

    protected override async Task SetUserOwner(Guid id, Guid organizationId, Guid userId)
    {
        DbContext.UserStackRoleAssignments.Add(new UserStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StackId = id,
            UserId = userId,
            RoleName = StackRole.Owner
        });
    }
    
    protected override List<object> AdditionalCreateMessages(Entities.Definition.Stack entity)
    {
        var messages = new List<object>();
        messages.Add(new StackModifiedEvent { Id = entity.Id, OrganizationId = entity.OrganizationId });
        return messages;
    }

    protected override List<object> AdditionalUpdateMessages(Entities.Definition.Stack entity)
    {
        var messages = new List<object>();
        messages.Add(new StackModifiedEvent { Id = entity.Id, OrganizationId = entity.OrganizationId });
        return messages;
    }


    protected override StackReadDto MapToDto(Entities.Definition.Stack entity)
    {
        return StackMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(Entities.Definition.Stack entity)
    {
        var currentCount = await DbContext.Stacks
            .CountAsync(s => s.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.StackQuota), currentCount);
    }

    public override async Task<Stack> ExecuteCreate(Stack entity)
    {
        NameValidator.EnsureValid(entity.Name, "Stack");
        return await base.ExecuteCreate(entity);
    }

    public override async Task<Stack> ExecuteUpdate(Stack entity)
    {
        NameValidator.EnsureValid(entity.Name, "Stack");
        return await base.ExecuteUpdate(entity);
    }

    public override async Task ExecuteDelete(Guid id, Guid organizationId)
    {
        // The Namespaces and their Modules go by cascade rather than through their own
        // repositories, so what a Module cannot take with it is cleared here too.
        await DbContext.ClearRowsBlockingModuleDelete(m => m.Namespace.StackId == id, organizationId);

        await DbContext.ClearRowsBlockingNamespaceDelete(n => n.StackId == id, organizationId);

        // A stack secret is restricted from the stack that scopes it.
        await DbContext.Set<StackSecret>()
            .Where(x => x.OrganizationId == organizationId && x.StackId == id)
            .ExecuteDeleteAsync();

        await base.ExecuteDelete(id, organizationId);
    }

    public async Task<Entities.Definition.Stack> GetByName(string name, Guid organizationId)
    {
        var entity = await DbContext.Stacks
            .Where(s => s.OrganizationId == organizationId)
            .SingleOrDefaultAsync(s => s.Name == name);

        if (entity == null) throw new EntityNotFoundException($"${nameof(Entities.Definition.Stack)} with Name {name} not found.");

        return entity;
    }
}