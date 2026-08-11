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
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Integration.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.RoleAssignments;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;

// ---- Base (Get / List / Delete only) ----
public class IntegrationRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
{
    public IntegrationRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class IntegrationRoleAssignmentRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
    : GenericIntegrationChildRepository<IntegrationRoleAssignment, IntegrationRoleAssignmentReadDto,
        IntegrationRoleAssignmentCreatedEvent, IntegrationRoleAssignmentUpdatedEvent, IntegrationRoleAssignmentDeletedEvent,
        IntegrationRoleAssignmentRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationRoleAssignmentReadDto MapToDto(IntegrationRoleAssignment entity)
        => IntegrationRoleAssignmentMapper.ToDto(entity);

    public override Task<IntegrationRoleAssignment> ExecuteCreate(IntegrationRoleAssignment entity)
        => throw new NotImplementedByDesignException("IntegrationRoleAssignmentRepository is for Get/List/Delete only; use a concrete per-principal repository for Create/Update.");

    public override Task<IntegrationRoleAssignment> ExecuteUpdate(IntegrationRoleAssignment entity)
        => throw new NotImplementedByDesignException("IntegrationRoleAssignmentRepository is for Get/List/Delete only; use a concrete per-principal repository for Create/Update.");
}

// ---- User ----
public class UserIntegrationRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
{
    public UserIntegrationRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class UserIntegrationRoleAssignmentRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
    : GenericIntegrationChildRepository<UserIntegrationRoleAssignment, UserIntegrationRoleAssignmentReadDto,
        UserIntegrationRoleAssignmentCreatedEvent, UserIntegrationRoleAssignmentUpdatedEvent, UserIntegrationRoleAssignmentDeletedEvent,
        IntegrationRoleAssignmentRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override UserIntegrationRoleAssignmentReadDto MapToDto(UserIntegrationRoleAssignment entity)
        => UserIntegrationRoleAssignmentMapper.ToDto(entity);
}

// ---- ServicePrincipal ----
public class ServicePrincipalIntegrationRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
{
    public ServicePrincipalIntegrationRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class ServicePrincipalIntegrationRoleAssignmentRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
    : GenericIntegrationChildRepository<ServicePrincipalIntegrationRoleAssignment, ServicePrincipalIntegrationRoleAssignmentReadDto,
        ServicePrincipalIntegrationRoleAssignmentCreatedEvent, ServicePrincipalIntegrationRoleAssignmentUpdatedEvent, ServicePrincipalIntegrationRoleAssignmentDeletedEvent,
        IntegrationRoleAssignmentRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override ServicePrincipalIntegrationRoleAssignmentReadDto MapToDto(ServicePrincipalIntegrationRoleAssignment entity)
        => ServicePrincipalIntegrationRoleAssignmentMapper.ToDto(entity);
}

// ---- Group ----
public class GroupIntegrationRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
{
    public GroupIntegrationRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class GroupIntegrationRoleAssignmentRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationRoleAssignmentRepositorySettings> options)
    : GenericIntegrationChildRepository<GroupIntegrationRoleAssignment, GroupIntegrationRoleAssignmentReadDto,
        GroupIntegrationRoleAssignmentCreatedEvent, GroupIntegrationRoleAssignmentUpdatedEvent, GroupIntegrationRoleAssignmentDeletedEvent,
        IntegrationRoleAssignmentRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override GroupIntegrationRoleAssignmentReadDto MapToDto(GroupIntegrationRoleAssignment entity)
        => GroupIntegrationRoleAssignmentMapper.ToDto(entity);
}
