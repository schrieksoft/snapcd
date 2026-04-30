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

public class GroupModuleRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupModuleRoleAssignmentRepositorySettings> options)
{
    public GroupModuleRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupModuleRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupModuleRoleAssignmentRepository : GenericModuleChildRepository<GroupModuleRoleAssignment, GroupModuleRoleAssignmentReadDto, GroupModuleRoleAssignmentCreatedEvent,
    GroupModuleRoleAssignmentUpdatedEvent, GroupModuleRoleAssignmentDeletedEvent, GroupModuleRoleAssignmentRepositorySettings>
{
    public GroupModuleRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupModuleRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupModuleRoleAssignmentReadDto MapToDto(GroupModuleRoleAssignment entity)
    {
        return GroupModuleRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(GroupModuleRoleAssignment entity)
    {
        var currentCount = await DbContext.GroupModuleRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.GroupModuleRoleAssignmentQuota), currentCount);
    }

    public async Task<List<GroupModuleRoleAssignment>> ListByGroup(Guid groupId, Guid organizationId)
    {
        return await DbContext.GroupModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<List<GroupModuleRoleAssignment>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await DbContext.GroupModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.ModuleId == moduleId)
            .ToListAsync();
    }

    public async Task<List<GroupModuleRoleAssignment>> ListByRole(ModuleRole role, Guid organizationId)
    {
        return await DbContext.GroupModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}