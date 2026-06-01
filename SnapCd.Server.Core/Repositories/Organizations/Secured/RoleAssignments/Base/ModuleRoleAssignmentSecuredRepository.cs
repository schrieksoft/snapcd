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
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;

public class ModuleRoleAssignmentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleRoleAssignmentRepositorySettings> options)
{
    public ModuleRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleRoleAssignmentSecuredRepository(
            new ModuleRoleAssignmentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleRoleAssignmentSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleRoleAssignment,
    ModuleRoleAssignmentReadDto,
    ModuleRoleAssignmentRepository,
    ModuleRoleAssignmentCreatedEvent,
    ModuleRoleAssignmentUpdatedEvent,
    ModuleRoleAssignmentDeletedEvent,
    ModuleRoleAssignmentRepositorySettings>
{
    public ModuleRoleAssignmentSecuredRepository(
        ModuleRoleAssignmentRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.IdentityAccessManager]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.IdentityAccessManager]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.IdentityAccessManager]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.IdentityAccessManager]
    };

    public override async Task<ModuleRoleAssignment> Create(ModuleRoleAssignment entity, bool inTransaction = true)
    {
        throw new NotImplementedByDesignException("ModuleRoleAssignmentSecuredRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<ModuleRoleAssignment> Update(ModuleRoleAssignment entity, bool inTransaction = true)
    {
        throw new NotImplementedByDesignException("ModuleRoleAssignmentSecuredRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}