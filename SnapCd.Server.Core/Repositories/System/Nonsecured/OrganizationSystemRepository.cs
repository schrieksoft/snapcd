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
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Settings.DataSeeder;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.Organizations;
using SnapCd.Server.Core.Dtos.OrganizationUsers;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.Repository.System;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Mappers.Repositories;
using SnapCd.Server.Core.Mappers.RoleAssignments;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.System.Nonsecured;

public class OrganizationRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OrganizationRepositorySettings> options,
    IUserQuotaProvider userQuotaProvider,
    IOrganizationLimitPolicy organizationLimitPolicy)
{
    public OrganizationSystemRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OrganizationSystemRepository(dbContext, principalProvider, bus, options, userQuotaProvider, organizationLimitPolicy);
    }
}

public class OrganizationSystemRepository : GenericSystemRepository<Organization, OrganizationReadDto, OrganizationCreatedEvent, OrganizationUpdatedEvent, OrganizationDeletedEvent,
    OrganizationRepositorySettings>
{
    private readonly IUserQuotaProvider _userQuotaProvider;
    private readonly IOrganizationLimitPolicy _organizationLimitPolicy;

    public OrganizationSystemRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OrganizationRepositorySettings> options,
        IUserQuotaProvider userQuotaProvider,
        IOrganizationLimitPolicy organizationLimitPolicy)
        : base(dbContext, principalProvider, bus, options)
    {
        _userQuotaProvider = userQuotaProvider;
        _organizationLimitPolicy = organizationLimitPolicy;
    }

    protected override Func<IQueryable<Organization>, IQueryable<Organization>> ByParentIdQueryModifier(Guid parentId)
    {
        // Organizations have no parent, so return a query that filters nothing
        return q => q;
    }

    protected override OrganizationReadDto MapToDto(Organization entity)
    {
        return OrganizationMapper.ToDto(entity);
    }

    public async Task<List<Organization>> ListWithFilter(bool includeDeleted = false,
        Func<IQueryable<Organization>, IQueryable<Organization>>? queryFilter = null)
    {
        var query = DbContext.Organizations.AsQueryable();

        if (!includeDeleted) query = query.Where(o => o.DeletedDateTime == null);

        query = query.Include(o => o.OrganizationUsers);

        if (queryFilter != null) query = queryFilter(query);

        return await query
            .OrderBy(o => o.Name)
            .ToListAsync();
    }

    public async Task<bool> SoftDelete(Guid id, Guid deletedByUserId)
    {
        var organization = await Get(id);
        if (organization == null) return false;

        organization.DeletedDateTime = DateTime.UtcNow;
        organization.DeletedByUserId = deletedByUserId;

        await DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<Organization> CreateWithOwner(string name, Guid createdByUserId)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;

            // Enforce organization limit based on edition (exclude System org from count)
            var totalOrgCount = await DbContext.Organizations.CountAsync(
                o => o.DeletedDateTime == null && o.Id != PreseededSettings.DefaultId);
            await _organizationLimitPolicy.EnforceAsync(totalOrgCount);

            // Check organization creation quota for the user
            // Count organizations where user is both member AND creator, not deleted
            var createdOrgCount = await DbContext.OrganizationUsers
                .Include(ou => ou.Organization)
                .Where(ou => ou.UserId == createdByUserId && !ou.IsDeactivated)
                .Where(ou => ou.Organization.CreatedBy == createdByUserId
                          && ou.Organization.CreatedByPrincipalDiscriminator == AuditPrincipalDiscriminator.User
                          && ou.Organization.DeletedDateTime == null)
                .CountAsync();

            // Get user's quota limit (NoOp returns unbounded for self-hosted; SaaS combines settings + per-user override)
            var quotaLimit = await _userQuotaProvider.GetOrganizationQuotaAsync(createdByUserId);

            if (createdOrgCount >= quotaLimit)
            {
                throw new QuotaExceededException(
                    "Organization",
                    createdOrgCount,
                    quotaLimit,
                    $"Organization creation quota exceeded. You have created {createdOrgCount} organizations, limit is {quotaLimit}.");
            }

            // Check if this is the user's first organization (before adding the new one)
            var hasExistingMemberships = await DbContext.Set<OrganizationUser>()
                .AnyAsync(m => m.UserId == createdByUserId && !m.IsDeactivated);

            // Create Organization with audit fields (no subscription yet — paid Cloud subscription required to use)
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Status = OrganizationStatus.Active,
                CreatedBy = createdByUserId,
                CreatedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
                CreatedDateTime = now,
                ModifiedBy = createdByUserId,
                ModifiedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
                ModifiedDateTime = now
            };

            DbContext.Organizations.Add(organization);

            // Create OrganizationUser with audit fields
            var orgUser = new OrganizationUser
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = createdByUserId,
                JoinedAt = now,
                IsDeactivated = false,
                InvitationCompleted = true,
                InvitationCompletedDateTime = now,
                CreatedBy = createdByUserId,
                CreatedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
                CreatedDateTime = now,
                ModifiedBy = createdByUserId,
                ModifiedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
                ModifiedDateTime = now
            };

            DbContext.Set<OrganizationUser>().Add(orgUser);

            // Create Owner role assignment with audit fields
            var ownerRoleAssignment = new UserOrganizationRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = createdByUserId,
                RoleName = OrganizationRole.Owner,
                PrincipalDiscriminator = RoleAssignmentPrincipalDiscriminator.User,
                CreatedBy = createdByUserId,
                CreatedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
                CreatedDateTime = now,
                ModifiedBy = createdByUserId,
                ModifiedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
                ModifiedDateTime = now
            };

            DbContext.Set<UserOrganizationRoleAssignment>().Add(ownerRoleAssignment);

            // Single SaveChangesAsync for all entities
            await DbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            // Emit events after successful commit
            await Bus.Publish(EventMapper.ToSystemCreateEto<Organization, OrganizationReadDto, OrganizationCreatedEvent>(organization, MapToDto),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; });

            await Bus.Publish(EventMapper.ToCreateEto<OrganizationUser, OrganizationUserReadDto, OrganizationUserCreatedEvent>(orgUser, OrganizationUserMapper.ToDto, organization.Id),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; });

            await Bus.Publish(EventMapper.ToCreateEto<UserOrganizationRoleAssignment, UserOrganizationRoleAssignmentReadDto, UserOrganizationRoleAssignmentCreatedEvent>(ownerRoleAssignment, UserOrganizationRoleAssignmentMapper.ToDto, organization.Id),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; });

            return organization;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}