using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.GroupMembers.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.GroupMembers;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.GroupMembers;

public class GroupMemberSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<GroupMemberRepositorySettings> options)
{
    public GroupMemberSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupMemberSecuredRepository(
            new GroupMemberRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class GroupMemberSecuredRepository : GenericOrganizationChildSecuredRepository<
    GroupMember,
    GroupMemberReadDto,
    GroupMemberRepository,
    GroupMemberCreatedEvent,
    GroupMemberUpdatedEvent,
    GroupMemberDeletedEvent,
    GroupMemberRepositorySettings>
{
    public GroupMemberSecuredRepository(
        GroupMemberRepository repository,
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


    public override async Task<GroupMember> Create(GroupMember entity, bool inTransaction = true)
    {
        throw new NotImplementedByDesignException("GroupMemberSecuredRepository can only get used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<GroupMember> Update(GroupMember entity, bool inTransaction = true)
    {
        throw new NotImplementedByDesignException("GroupMemberSecuredRepository can only get used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}