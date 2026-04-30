using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.GroupMembers.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.GroupMembers.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.GroupMembers;

public class GroupMemberRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupMemberRepositorySettings> options)
{
    public GroupMemberRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupMemberRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupMemberRepository : GenericOrganizationChildRepository<GroupMember, GroupMemberReadDto, GroupMemberCreatedEvent, GroupMemberUpdatedEvent, GroupMemberDeletedEvent,
    GroupMemberRepositorySettings>
{
    private const int MaxGroupHierarchyDepth = 10;

    public GroupMemberRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupMemberRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupMemberReadDto MapToDto(GroupMember entity)
    {
        return GroupMemberMapper.ToDto(entity);
    }

    public override async Task<GroupMember> ExecuteCreate(GroupMember entity)
    {
        throw new NotImplementedByDesignException("GroupMemberRepository can only get used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<GroupMember> ExecuteUpdate(GroupMember entity)
    {
        throw new NotImplementedByDesignException("GroupMemberRepository can only get used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}