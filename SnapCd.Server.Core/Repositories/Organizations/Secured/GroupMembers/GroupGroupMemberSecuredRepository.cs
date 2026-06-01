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
using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.GroupMembers;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.GroupMembers;

public class GroupGroupMemberSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<GroupGroupMemberRepositorySettings> options)
{
    public GroupGroupMemberSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupGroupMemberSecuredRepository(
            new GroupGroupMemberRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class GroupGroupMemberSecuredRepository : GenericOrganizationChildSecuredRepository<
    GroupGroupMember,
    GroupGroupMemberReadDto,
    GroupGroupMemberRepository,
    GroupGroupMemberCreatedEvent,
    GroupGroupMemberUpdatedEvent,
    GroupGroupMemberDeletedEvent,
    GroupGroupMemberRepositorySettings>
{
    public GroupGroupMemberSecuredRepository(
        GroupGroupMemberRepository repository,
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

    public async Task<List<GroupGroupMember>> ListByGroupId(Guid groupId, Guid organizationId)
    {
        return await Repository.ListByGroupId(groupId, organizationId, ReadQuery(organizationId));
    }

    public async Task<GroupGroupMember> GetByGroupIds(Guid parentGroupId, Guid memberGroupId, Guid organizationId)
    {
        var entity = await Repository.GetByGroupIds(parentGroupId, memberGroupId, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(GroupGroupMember)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }
}