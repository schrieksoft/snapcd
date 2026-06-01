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
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.Base;

public class StackRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<StackRoleAssignmentRepositorySettings> options)
{
    public StackRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StackRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class StackRoleAssignmentRepository : GenericStackChildRepository<StackRoleAssignment, StackRoleAssignmentDto, StackRoleAssignmentCreatedEvent, StackRoleAssignmentUpdatedEvent,
    StackRoleAssignmentDeletedEvent, StackRoleAssignmentRepositorySettings>
{
    public StackRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<StackRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override StackRoleAssignmentDto MapToDto(StackRoleAssignment entity)
    {
        return StackRoleAssignmentMapper.ToDto(entity);
    }

    public override async Task<StackRoleAssignment> ExecuteCreate(StackRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("StackRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<StackRoleAssignment> ExecuteUpdate(StackRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("StackRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}