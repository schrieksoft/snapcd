using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    VariableSetReadDto,
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
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader],
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
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner]
    };

    public async Task<VariableSet> Get(Guid moduleId, string checksum, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, checksum, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to VariableSet {entity.Id}");

        return entity;
    }

    public async Task<VariableSet> GetLatestByModuleId(Guid moduleId, Guid organizationId)
    {
        var entity = await Repository.GetLatestByModuleId(moduleId, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to VariableSet {entity.Id}");

        return entity;
    }

    public async Task<List<VariableSet>> ListSetsByIds(List<Guid> variableSetIds, Guid organizationId)
    {
        var variableSets = await Repository.ListSetsByIds(variableSetIds, organizationId);

        foreach (var variableSet in variableSets)
            if (!CanRead(variableSet.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to VariableSet {variableSet.Id}");

        return variableSets;
    }

    public async Task<Guid?> CreateWithVariables(VariableSet variableSet, Guid organizationId)
    {
        if (!CanCreate(variableSet.ModuleId, organizationId))
            throw new UnauthorizedAccessException($"Access denied to create VariableSet for module {variableSet.ModuleId}");

        return await Repository.CreateWithVariables(variableSet, organizationId);
    }
}