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

public class UserModuleRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<UserModuleRoleAssignmentRepositorySettings> options)
{
    public UserModuleRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserModuleRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class UserModuleRoleAssignmentRepository : GenericModuleChildRepository<UserModuleRoleAssignment, UserModuleRoleAssignmentReadDto, UserModuleRoleAssignmentCreatedEvent,
    UserModuleRoleAssignmentUpdatedEvent, UserModuleRoleAssignmentDeletedEvent, UserModuleRoleAssignmentRepositorySettings>
{
    public UserModuleRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<UserModuleRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override UserModuleRoleAssignmentReadDto MapToDto(UserModuleRoleAssignment entity)
    {
        return UserModuleRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(UserModuleRoleAssignment entity)
    {
        var currentCount = await DbContext.UserModuleRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.UserModuleRoleAssignmentQuota), currentCount);
    }

    public async Task<List<UserModuleRoleAssignment>> ListByUser(Guid userId, Guid organizationId)
    {
        return await DbContext.UserModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<UserModuleRoleAssignment>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await DbContext.UserModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.ModuleId == moduleId)
            .ToListAsync();
    }

    public async Task<List<UserModuleRoleAssignment>> ListByRole(ModuleRole role, Guid organizationId)
    {
        return await DbContext.UserModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}