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
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using System.Linq.Expressions;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Variables;

public class VariableSetSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<VariableSetRepositorySettings> options,
    QuotaService quotaService)
{
    public VariableSetSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new VariableSetSecuredRepository(
            new VariableSetRepository(dbContext, principalProvider, bus, options, quotaService),
            principalProvider);
    }
}

public class VariableSetSecuredRepository : GenericModuleChildSecuredRepository<
    VariableSet,
    VariableSetReadDto, VariableSetMetadata,
    VariableSetRepository,
    VariableSetCreatedEvent,
    VariableSetUpdatedEvent,
    VariableSetDeletedEvent,
    VariableSetRepositorySettings>
{
    public VariableSetSecuredRepository(
        VariableSetRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.StackContributor, OrganizationRole.StackReader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = [],
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = []
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner]
    };

    public async Task<VariableSet> Get(Guid moduleId, string checksum, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, checksum, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(VariableSet)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

    public async Task<VariableSet> GetLatestByModuleId(Guid moduleId, Guid organizationId)
    {
        var entity = await Repository.GetLatestByModuleId(moduleId, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(VariableSet)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

    public async Task<List<VariableSet>> ListSetsByIds(List<Guid> variableSetIds, Guid organizationId)
    {
        var variableSets = await Repository.ListSetsByIds(variableSetIds, organizationId);

        foreach (var variableSet in variableSets)
            if (!CanRead(variableSet.Id, organizationId))
                throw new PrincipalNotAuthorizedException(
                    $"{nameof(VariableSet)} with ID {variableSet.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return variableSets;
    }

    public async Task<Guid?> CreateWithVariables(VariableSet variableSet, Guid organizationId)
    {
        if (!CanCreate(variableSet.ModuleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Module with ID {variableSet.ModuleId} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to create a {nameof(VariableSet)} within it.");

        return await Repository.CreateWithVariables(variableSet, organizationId);
    }
}