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

public class ServicePrincipalGroupMemberSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ServicePrincipalGroupMemberRepositorySettings> options)
{
    public ServicePrincipalGroupMemberSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalGroupMemberSecuredRepository(
            new ServicePrincipalGroupMemberRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ServicePrincipalGroupMemberSecuredRepository : GenericOrganizationChildSecuredRepository<
    ServicePrincipalGroupMember,
    ServicePrincipalGroupMemberReadDto,
    ServicePrincipalGroupMemberRepository,
    ServicePrincipalGroupMemberCreatedEvent,
    ServicePrincipalGroupMemberUpdatedEvent,
    ServicePrincipalGroupMemberDeletedEvent,
    ServicePrincipalGroupMemberRepositorySettings>
{
    public ServicePrincipalGroupMemberSecuredRepository(
        ServicePrincipalGroupMemberRepository repository,
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

    public async Task<List<ServicePrincipalGroupMember>> ListByGroupId(Guid groupId, Guid organizationId)
    {
        return await Repository.ListByGroupId(groupId, organizationId, ReadQuery(organizationId));
    }

    public async Task<ServicePrincipalGroupMember> GetByParents(Guid groupId, Guid servicePrincipalId, Guid organizationId)
    {
        var entity = await Repository.GetByParents(groupId, servicePrincipalId, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(ServicePrincipalGroupMember)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }
}