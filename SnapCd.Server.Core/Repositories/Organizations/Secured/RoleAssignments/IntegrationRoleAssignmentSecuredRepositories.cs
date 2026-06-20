// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Integration.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;

// ---- Base (Get / List / Delete) — org IAM gated ----
public class IntegrationRoleAssignmentSecuredRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
{
    public IntegrationRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());
        return new IntegrationRoleAssignmentSecuredRepository(
            new IntegrationRoleAssignmentRepository(dbFactory.CreateDbContext(), principalProvider, bus, options), principalProvider);
    }
}

public class IntegrationRoleAssignmentSecuredRepository(IntegrationRoleAssignmentRepository repository, IPrincipalProvider principalProvider)
    : GenericOrganizationChildSecuredRepository<IntegrationRoleAssignment, IntegrationRoleAssignmentReadDto, IntegrationRoleAssignmentRepository,
        IntegrationRoleAssignmentCreatedEvent, IntegrationRoleAssignmentUpdatedEvent, IntegrationRoleAssignmentDeletedEvent,
        IntegrationRoleAssignmentRepositorySettings>(repository, principalProvider)
{
    public override PermissionMap ReadPermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager] };
    public override PermissionMap UpdatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager] };
    public override PermissionMap CreatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager] };
    public override PermissionMap DeletePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager] };
}

// ---- User ----
public class UserIntegrationRoleAssignmentSecuredRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
{
    public UserIntegrationRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());
        return new UserIntegrationRoleAssignmentSecuredRepository(
            new UserIntegrationRoleAssignmentRepository(dbFactory.CreateDbContext(), principalProvider, bus, options), principalProvider);
    }
}

public class UserIntegrationRoleAssignmentSecuredRepository(UserIntegrationRoleAssignmentRepository repository, IPrincipalProvider principalProvider)
    : GenericIntegrationChildSecuredRepository<UserIntegrationRoleAssignment, UserIntegrationRoleAssignmentReadDto, UserIntegrationRoleAssignmentRepository,
        UserIntegrationRoleAssignmentCreatedEvent, UserIntegrationRoleAssignmentUpdatedEvent, UserIntegrationRoleAssignmentDeletedEvent,
        IntegrationRoleAssignmentRepositorySettings>(repository, principalProvider)
{
    public override PermissionMap ReadPermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap UpdatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap CreatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap DeletePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
}

// ---- ServicePrincipal ----
public class ServicePrincipalIntegrationRoleAssignmentSecuredRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
{
    public ServicePrincipalIntegrationRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());
        return new ServicePrincipalIntegrationRoleAssignmentSecuredRepository(
            new ServicePrincipalIntegrationRoleAssignmentRepository(dbFactory.CreateDbContext(), principalProvider, bus, options), principalProvider);
    }
}

public class ServicePrincipalIntegrationRoleAssignmentSecuredRepository(ServicePrincipalIntegrationRoleAssignmentRepository repository, IPrincipalProvider principalProvider)
    : GenericIntegrationChildSecuredRepository<ServicePrincipalIntegrationRoleAssignment, ServicePrincipalIntegrationRoleAssignmentReadDto, ServicePrincipalIntegrationRoleAssignmentRepository,
        ServicePrincipalIntegrationRoleAssignmentCreatedEvent, ServicePrincipalIntegrationRoleAssignmentUpdatedEvent, ServicePrincipalIntegrationRoleAssignmentDeletedEvent,
        IntegrationRoleAssignmentRepositorySettings>(repository, principalProvider)
{
    public override PermissionMap ReadPermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap UpdatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap CreatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap DeletePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
}

// ---- Group ----
public class GroupIntegrationRoleAssignmentSecuredRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
{
    public GroupIntegrationRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());
        return new GroupIntegrationRoleAssignmentSecuredRepository(
            new GroupIntegrationRoleAssignmentRepository(dbFactory.CreateDbContext(), principalProvider, bus, options), principalProvider);
    }
}

public class GroupIntegrationRoleAssignmentSecuredRepository(GroupIntegrationRoleAssignmentRepository repository, IPrincipalProvider principalProvider)
    : GenericIntegrationChildSecuredRepository<GroupIntegrationRoleAssignment, GroupIntegrationRoleAssignmentReadDto, GroupIntegrationRoleAssignmentRepository,
        GroupIntegrationRoleAssignmentCreatedEvent, GroupIntegrationRoleAssignmentUpdatedEvent, GroupIntegrationRoleAssignmentDeletedEvent,
        IntegrationRoleAssignmentRepositorySettings>(repository, principalProvider)
{
    public override PermissionMap ReadPermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap UpdatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap CreatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
    public override PermissionMap DeletePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager], IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.IdentityAccessManager] };
}
