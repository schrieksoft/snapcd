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

public class UserNamespaceRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<UserNamespaceRoleAssignmentRepositorySettings> options)
{
    public UserNamespaceRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserNamespaceRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class UserNamespaceRoleAssignmentRepository : GenericNamespaceChildRepository<UserNamespaceRoleAssignment, UserNamespaceRoleAssignmentReadDto, UserNamespaceRoleAssignmentCreatedEvent,
    UserNamespaceRoleAssignmentUpdatedEvent, UserNamespaceRoleAssignmentDeletedEvent, UserNamespaceRoleAssignmentRepositorySettings>
{
    public UserNamespaceRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<UserNamespaceRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override UserNamespaceRoleAssignmentReadDto MapToDto(UserNamespaceRoleAssignment entity)
    {
        return UserNamespaceRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(UserNamespaceRoleAssignment entity)
    {
        var currentCount = await DbContext.UserNamespaceRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.UserNamespaceRoleAssignmentQuota), currentCount);
    }

    public async Task<List<UserNamespaceRoleAssignment>> ListByUser(Guid userId, Guid organizationId)
    {
        return await DbContext.UserNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<UserNamespaceRoleAssignment>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await DbContext.UserNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.NamespaceId == namespaceId)
            .ToListAsync();
    }

    public async Task<List<UserNamespaceRoleAssignment>> ListByRole(NamespaceRole role, Guid organizationId)
    {
        return await DbContext.UserNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}