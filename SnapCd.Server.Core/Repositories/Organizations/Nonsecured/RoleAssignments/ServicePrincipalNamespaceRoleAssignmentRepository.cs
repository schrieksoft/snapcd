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
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;

public class ServicePrincipalNamespaceRoleAssignmentRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ServicePrincipalNamespaceRoleAssignmentRepositorySettings> options)
{
    public ServicePrincipalNamespaceRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalNamespaceRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class ServicePrincipalNamespaceRoleAssignmentRepository : GenericNamespaceChildRepository<ServicePrincipalNamespaceRoleAssignment, ServicePrincipalNamespaceRoleAssignmentReadDto,
    ServicePrincipalNamespaceRoleAssignmentCreatedEvent, ServicePrincipalNamespaceRoleAssignmentUpdatedEvent, ServicePrincipalNamespaceRoleAssignmentDeletedEvent,
    ServicePrincipalNamespaceRoleAssignmentRepositorySettings>
{
    public ServicePrincipalNamespaceRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ServicePrincipalNamespaceRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ServicePrincipalNamespaceRoleAssignmentReadDto MapToDto(ServicePrincipalNamespaceRoleAssignment entity)
    {
        return ServicePrincipalNamespaceRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ServicePrincipalNamespaceRoleAssignment entity)
    {
        var currentCount = await DbContext.ServicePrincipalNamespaceRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ServicePrincipalNamespaceRoleAssignmentQuota), currentCount);
    }

    public async Task<List<ServicePrincipalNamespaceRoleAssignment>> ListByServicePrincipal(Guid servicePrincipalId, Guid organizationId)
    {
        return await DbContext.ServicePrincipalNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.ServicePrincipalId == servicePrincipalId)
            .ToListAsync();
    }

    public async Task<List<ServicePrincipalNamespaceRoleAssignment>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await DbContext.ServicePrincipalNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.NamespaceId == namespaceId)
            .ToListAsync();
    }

    public async Task<List<ServicePrincipalNamespaceRoleAssignment>> ListByRole(NamespaceRole role, Guid organizationId)
    {
        return await DbContext.ServicePrincipalNamespaceRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}