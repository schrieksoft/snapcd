using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.OrganizationUsers;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class OrganizationUserRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OrganizationUserRepositorySettings> options)
{
    public OrganizationUserRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OrganizationUserRepository(dbContext, principalProvider, bus, options);
    }
}

public class OrganizationUserRepository : GenericOrganizationChildRepository<OrganizationUser, OrganizationUserReadDto, OrganizationUserCreatedEvent, OrganizationUserUpdatedEvent,
    OrganizationUserDeletedEvent, OrganizationUserRepositorySettings>
{
    public OrganizationUserRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OrganizationUserRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override OrganizationUserReadDto MapToDto(OrganizationUser entity)
    {
        return OrganizationUserMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(OrganizationUser entity)
    {
        var currentCount = await DbContext.OrganizationUsers
            .CountAsync(ou => ou.OrganizationId == entity.OrganizationId && !ou.IsDeactivated);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.OrganizationUserQuota), currentCount);
    }

    protected override Func<IQueryable<OrganizationUser>, IQueryable<OrganizationUser>> ByParentIdQueryModifier(Guid parentId)
    {
        return query => query.Where(ou => ou.OrganizationId == parentId);
    }

    /// <summary>
    /// Gets all organization users
    /// </summary>
    /// <param name="queryFilter">Optional query filter to apply to users at database level</param>
    /// <returns>List of organization users</returns>
    public async Task<List<OrganizationUser>> List(Func<IQueryable<OrganizationUser>, IQueryable<OrganizationUser>>? queryFilter = null)
    {
        var query = DbContext.OrganizationUsers.AsQueryable();

        query = query.Include(ou => ou.Organization)
            .Include(ou => ou.User);

        if (queryFilter != null) query = queryFilter(query);

        return await query
            .Where(ou => !ou.IsDeactivated)
            .OrderBy(ou => ou.Organization.Name)
            .ThenBy(ou => ou.User!.Email)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all organization users for a specific organization
    /// </summary>
    /// <param name="organizationId">Organization to filter on</param>
    /// <param name="queryFilter">Optional query filter to apply to users at database level</param>
    /// <returns>List of organization users</returns>
    public async Task<List<OrganizationUser>> ListByOrganizationId(Guid organizationId, Func<IQueryable<OrganizationUser>, IQueryable<OrganizationUser>>? queryFilter = null)
    {
        var query = DbContext.OrganizationUsers.AsQueryable();

        query = query.Include(ou => ou.Organization)
            .Include(ou => ou.User)
            .Where(ou => ou.OrganizationId == organizationId);

        if (queryFilter != null) query = queryFilter(query);

        return await query
            .Where(ou => !ou.IsDeactivated)
            .OrderBy(ou => ou.User!.Email)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all organizations for a specific user
    /// </summary>
    /// <param name="userId">User to filter on</param>
    /// <returns>List of organization users</returns>
    public async Task<List<OrganizationUser>> GetByUserIdAsync(Guid userId)
    {
        return await DbContext.OrganizationUsers
            .Include(ou => ou.Organization)
            .Where(ou => ou.UserId == userId && !ou.IsDeactivated && ou.InvitationCompleted)
            .OrderBy(ou => ou.Organization.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets an organization user by its ID
    /// </summary>
    /// <param name="id">The organization user ID</param>
    /// <returns>Organization user or null if not found</returns>
    public async Task<OrganizationUser?> Get(Guid id)
    {
        return await DbContext.OrganizationUsers
            .Include(ou => ou.Organization)
            .Include(ou => ou.User)
            .FirstOrDefaultAsync(ou => ou.Id == id && !ou.IsDeactivated);
    }

    /// <summary>
    /// Gets organization user by organization ID and user ID
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="userId">The user ID</param>
    /// <returns>Organization user or null if not found</returns>
    public async Task<OrganizationUser?> Get(Guid organizationId, Guid userId)
    {
        return await DbContext.OrganizationUsers
            .Include(ou => ou.Organization)
            .Include(ou => ou.User)
            .FirstOrDefaultAsync(ou => ou.OrganizationId == organizationId
                                       && ou.UserId == userId
                                       && !ou.IsDeactivated);
    }

    public async Task<OrganizationUser?> GetByUserId(Guid userId, Guid organizationId)
    {
        return await DbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == organizationId)
            .SingleOrDefaultAsync(ou => ou.UserId == userId);
    }

    /// <summary>
    /// Gets organization user by invitation token
    /// </summary>
    /// <param name="invitationToken">The invitation token</param>
    /// <returns>Organization user or null if not found</returns>
    public async Task<OrganizationUser?> GetByInvitationToken(string invitationToken)
    {
        return await DbContext.OrganizationUsers
            .Include(ou => ou.Organization)
            .Include(ou => ou.User)
            .FirstOrDefaultAsync(ou => ou.InvitationToken == invitationToken
                                       && !ou.InvitationCompleted
                                       && !ou.IsDeactivated
                                       && ou.InvitationExpirationDateTime > DateTime.UtcNow);
    }

    public async Task<OrganizationUser?> GetByInvitationToken(string invitationToken, Guid organizationId)
    {
        return await DbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == organizationId)
            .SingleOrDefaultAsync(ou => ou.InvitationToken == invitationToken);
    }

    public override async Task<OrganizationUser> ExecuteCreate(OrganizationUser organizationUser)
    {
        organizationUser.JoinedAt = DateTime.UtcNow;
        return await base.ExecuteCreate(organizationUser);
    }

    /// <summary>
    /// Deactivates an organization user (soft delete)
    /// </summary>
    /// <param name="id">The organization user ID to deactivate</param>
    /// <returns>True if organization user was found and deactivated, false otherwise</returns>
    public async Task<bool> Deactivate(Guid id)
    {
        var organizationUser = await Get(id);
        if (organizationUser == null) return false;

        var previousDto = MapToDto(organizationUser);
        organizationUser.IsDeactivated = true;
        organizationUser.ModifiedDateTime = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
        await PublishDeactivationEvent(organizationUser, previousDto);
        return true;
    }

    /// <summary>
    /// Deactivates an organization user by organization and user ID
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="userId">The user ID</param>
    /// <returns>True if organization user was found and deactivated, false otherwise</returns>
    public async Task<bool> Deactivate(Guid organizationId, Guid userId)
    {
        var organizationUser = await Get(organizationId, userId);
        if (organizationUser == null) return false;

        var previousDto = MapToDto(organizationUser);
        organizationUser.IsDeactivated = true;
        organizationUser.ModifiedDateTime = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
        await PublishDeactivationEvent(organizationUser, previousDto);
        return true;
    }

    private async Task PublishDeactivationEvent(OrganizationUser organizationUser, OrganizationUserReadDto previousDto)
    {
        if (!Options.Value.EmitCreateEvents) return;

        var updateEvent = new OrganizationUserUpdatedEvent
        {
            PreviousData = previousDto,
            Data = MapToDto(organizationUser),
            OrganizationId = organizationUser.OrganizationId,
            CreatedBy = organizationUser.CreatedBy,
            CreatedByPrincipalDiscriminator = organizationUser.CreatedByPrincipalDiscriminator,
            CreatedDateTime = organizationUser.CreatedDateTime,
            ModifiedBy = organizationUser.ModifiedBy,
            ModifiedByPrincipalDiscriminator = organizationUser.ModifiedByPrincipalDiscriminator,
            ModifiedDateTime = organizationUser.ModifiedDateTime,
            PreviousModifiedBy = organizationUser.ModifiedBy,
            PreviousModifiedByPrincipalDiscriminator = organizationUser.ModifiedByPrincipalDiscriminator,
            PreviousModifiedDateTime = organizationUser.ModifiedDateTime,
        };

        await Bus.Publish(updateEvent,
            publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; });
    }

    /// <summary>
    /// Counts the total number of organization users for an organization
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <returns>Count of organization users</returns>
    public async Task<int> CountByOrganization(Guid organizationId)
    {
        return await DbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == organizationId && !ou.IsDeactivated)
            .CountAsync();
    }


    public bool UserHasOrganizationMembership(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return false;

        return DbContext.OrganizationUsers
            .Any(m => m.UserId == userGuid && !m.IsDeactivated);
    }


    /// <summary>
    /// Gets organization user by invitation token (including expired/deactivated)
    /// </summary>
    public async Task<OrganizationUser?> GetByInvitationTokenIncludingAll(string invitationToken)
    {
        return await DbContext.OrganizationUsers
            .Include(ou => ou.Organization)
            .Include(ou => ou.User)
            .FirstOrDefaultAsync(ou => ou.InvitationToken == invitationToken && !ou.InvitationCompleted);
    }

    /// <summary>
    /// Checks if a user is a member of an organization
    /// </summary>
    public async Task<bool> IsUserMember(Guid organizationId, Guid userId)
    {
        return await DbContext.OrganizationUsers
            .AnyAsync(ou => ou.OrganizationId == organizationId && ou.UserId == userId && !ou.IsDeactivated);
    }

    /// <summary>
    /// Counts other memberships for a user excluding a specific organization user
    /// </summary>
    public async Task<int> CountOtherMemberships(Guid userId, Guid excludeOrganizationUserId)
    {
        return await DbContext.OrganizationUsers
            .CountAsync(ou => ou.UserId == userId && ou.Id != excludeOrganizationUserId);
    }

    /// <summary>
    /// Gets user IDs that are members of an organization
    /// </summary>
    public async Task<List<Guid>> GetMemberUserIds(Guid organizationId)
    {
        return await DbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == organizationId && !ou.IsDeactivated)
            .Select(ou => ou.UserId)
            .ToListAsync();
    }

    /// <summary>
    /// Hard deletes an organization user
    /// </summary>
    public async Task Delete(OrganizationUser organizationUser)
    {
        DbContext.OrganizationUsers.Remove(organizationUser);
        await DbContext.SaveChangesAsync();
    }
}