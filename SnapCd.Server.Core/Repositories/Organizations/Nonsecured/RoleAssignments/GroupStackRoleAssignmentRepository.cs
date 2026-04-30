using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;

public class GroupStackRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupStackRoleAssignmentRepositorySettings> options)
{
    public GroupStackRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupStackRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupStackRoleAssignmentRepository : GenericStackChildRepository<GroupStackRoleAssignment, GroupStackRoleAssignmentReadDto, GroupStackRoleAssignmentCreatedEvent,
    GroupStackRoleAssignmentUpdatedEvent, GroupStackRoleAssignmentDeletedEvent, GroupStackRoleAssignmentRepositorySettings>
{
    public GroupStackRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupStackRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupStackRoleAssignmentReadDto MapToDto(GroupStackRoleAssignment entity)
    {
        return GroupStackRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(GroupStackRoleAssignment entity)
    {
        var currentCount = await DbContext.GroupStackRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.GroupStackRoleAssignmentQuota), currentCount);
    }

    public async Task<List<GroupStackRoleAssignment>> ListByGroup(Guid groupId, Guid organizationId)
    {
        return await DbContext.GroupStackRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<List<GroupStackRoleAssignment>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await DbContext.GroupStackRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.StackId == stackId)
            .ToListAsync();
    }

    public async Task<List<GroupStackRoleAssignment>> ListByRole(StackRole role, Guid organizationId)
    {
        return await DbContext.GroupStackRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}