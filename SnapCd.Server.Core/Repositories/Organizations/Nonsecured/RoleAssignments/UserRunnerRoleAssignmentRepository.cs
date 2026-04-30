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

public class UserRunnerRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<UserRunnerRoleAssignmentRepositorySettings> options)
{
    public UserRunnerRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserRunnerRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class UserRunnerRoleAssignmentRepository : GenericOrganizationChildRepository<UserRunnerRoleAssignment, UserRunnerRoleAssignmentReadDto, UserRunnerRoleAssignmentCreatedEvent,
    UserRunnerRoleAssignmentUpdatedEvent, UserRunnerRoleAssignmentDeletedEvent, UserRunnerRoleAssignmentRepositorySettings>
{
    public UserRunnerRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<UserRunnerRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override UserRunnerRoleAssignmentReadDto MapToDto(UserRunnerRoleAssignment entity)
    {
        return UserRunnerRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(UserRunnerRoleAssignment entity)
    {
        var currentCount = await DbContext.UserRunnerRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.UserRunnerRoleAssignmentQuota), currentCount);
    }

    public async Task<List<UserRunnerRoleAssignment>> ListByUser(Guid userId, Guid organizationId)
    {
        return await DbContext.UserRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<UserRunnerRoleAssignment>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await DbContext.UserRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RunnerId == runnerId)
            .ToListAsync();
    }

    public async Task<List<UserRunnerRoleAssignment>> ListByRole(RunnerRole role, Guid organizationId)
    {
        return await DbContext.UserRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}