using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Groups;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class GroupSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<GroupRepositorySettings> options)
{
    public GroupSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupSecuredRepository(
            new GroupRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class GroupSecuredRepository : GenericOrganizationChildSecuredRepository<
    Group,
    GroupReadDto,
    GroupRepository,
    GroupCreatedEvent,
    GroupUpdatedEvent,
    GroupDeletedEvent,
    GroupRepositorySettings>
{
    public GroupSecuredRepository(
        GroupRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public async Task<Group?> GetByName(string name, Guid organizationId)
    {
        var entity = await Repository.GetByName(name, organizationId);
        
        if (entity == null)
            throw new EntityNotFoundException($"Unable to find Group with name \"{name}\"");
        
        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to DependsOnModule {entity.Id}");

        return entity;

    }
}