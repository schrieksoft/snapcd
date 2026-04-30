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

public class GroupNamespaceRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupNamespaceRoleAssignmentRepositorySettings> options)
{
    public GroupNamespaceRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupNamespaceRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupNamespaceRoleAssignmentRepository : GenericNamespaceChildRepository<GroupNamespaceRoleAssignment, GroupNamespaceRoleAssignmentReadDto, GroupNamespaceRoleAssignmentCreatedEvent,
    GroupNamespaceRoleAssignmentUpdatedEvent, GroupNamespaceRoleAssignmentDeletedEvent, GroupNamespaceRoleAssignmentRepositorySettings>
{
    public GroupNamespaceRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupNamespaceRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupNamespaceRoleAssignmentReadDto MapToDto(GroupNamespaceRoleAssignment entity)
    {
        return GroupNamespaceRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(GroupNamespaceRoleAssignment entity)
    {
        var currentCount = await DbContext.GroupNamespaceRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.GroupNamespaceRoleAssignmentQuota), currentCount);
    }

    public async Task<List<GroupNamespaceRoleAssignment>> ListByGroup(Guid groupId, Guid organizationId)
    {
        return await DbContext.GroupNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<List<GroupNamespaceRoleAssignment>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await DbContext.GroupNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.NamespaceId == namespaceId)
            .ToListAsync();
    }

    public async Task<List<GroupNamespaceRoleAssignment>> ListByRole(NamespaceRole role, Guid organizationId)
    {
        return await DbContext.GroupNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}