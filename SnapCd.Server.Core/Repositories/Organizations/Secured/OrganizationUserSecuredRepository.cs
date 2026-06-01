// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.OrganizationUsers;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class OrganizationUserSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OrganizationUserRepositorySettings> options)
{
    public OrganizationUserSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OrganizationUserSecuredRepository(
            new OrganizationUserRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class OrganizationUserSecuredRepository : GenericOrganizationChildSecuredRepository<
    OrganizationUser,
    OrganizationUserReadDto,
    OrganizationUserRepository,
    OrganizationUserCreatedEvent,
    OrganizationUserUpdatedEvent,
    OrganizationUserDeletedEvent,
    OrganizationUserRepositorySettings>
{
    public OrganizationUserSecuredRepository(
        OrganizationUserRepository repository,
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

    /// <summary>
    /// Gets all organization users for a specific organization with permission checks
    /// </summary>
    /// <param name="organizationId">Organization to filter on</param>
    /// <returns>List of organization users</returns>
    public async Task<List<OrganizationUser>> ListByOrganizationId(Guid organizationId)
    {
        // Use the base List method which already applies permission checks via ReadQuery
        return await List(organizationId, query =>
                query.Include(ou => ou.Organization)
                    .Include(ou => ou.User)
                    .Where(ou => !ou.IsDeactivated),
            query => query.OrderBy(ou => ou.User!.Email));
    }

    /// <summary>
    /// Gets organization user by organization ID and user ID with permission check
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="userId">The user ID</param>
    /// <returns>Organization user or null if not found or no access</returns>
    public async Task<OrganizationUser?> Get(Guid organizationId, Guid userId)
    {
        var organizationUser = await Repository.Get(organizationId, userId);
        if (organizationUser == null)
            return null;

        if (!CanRead(organizationUser.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"OrganizationUser with organization ID {organizationId} and user ID {userId} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return organizationUser;
    }

    public async Task<OrganizationUser?> GetByUserId(Guid userId, Guid organizationId)
    {
        var entity =  await Repository.GetByUserId(userId, organizationId);
        
        if (entity == null)
            throw new EntityNotFoundException($"Unable to find OrganizationUser with UserId \"{userId}\"");
        
        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to OrganizationUser with UserId {userId}");

        return entity;
        
    }

    /// <summary>
    /// Gets organization user by invitation token with permission check
    /// </summary>
    /// <param name="invitationToken">The invitation token</param>
    /// <returns>Organization user or null if not found or no access</returns>
    public async Task<OrganizationUser?> GetByInvitationToken(string invitationToken)
    {
        var organizationUser = await Repository.GetByInvitationToken(invitationToken);
        if (organizationUser == null)
            return null;

        if (!CanRead(organizationUser.Id, organizationUser.OrganizationId))
            throw new PrincipalNotAuthorizedException(
                $"OrganizationUser with invitation token not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationUser.OrganizationId)} does not have permission to read it.");

        return organizationUser;
    }

    public async Task<OrganizationUser?> GetByInvitationToken(string invitationToken, Guid organizationId)
    {
        var organizationUser = await Repository.GetByInvitationToken(invitationToken, organizationId);
        if (organizationUser == null)
            return null;

        if (!CanRead(organizationUser.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"OrganizationUser with invitation token not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return organizationUser;
    }

    /// <summary>
    /// Deactivates an organization user with permission check
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="userId">The user ID</param>
    /// <returns>True if organization user was found and deactivated, false otherwise</returns>
    public async Task<bool> Deactivate(Guid organizationId, Guid userId)
    {
        var organizationUser = await Repository.Get(organizationId, userId);
        if (organizationUser == null)
            return false;

        if (!CanDelete(organizationUser.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"OrganizationUser with organization ID {organizationId} and user ID {userId} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to delete it.");

        return await Repository.Deactivate(organizationId, userId);
    }
}