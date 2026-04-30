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

public class OrganizationRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OrganizationRoleAssignmentRepositorySettings> options)
{
    public OrganizationRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OrganizationRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class OrganizationRoleAssignmentRepository : GenericOrganizationChildRepository<OrganizationRoleAssignment, OrganizationRoleAssignmentReadDto, OrganizationRoleAssignmentCreatedEvent,
    OrganizationRoleAssignmentUpdatedEvent, OrganizationRoleAssignmentDeletedEvent, OrganizationRoleAssignmentRepositorySettings>
{
    public OrganizationRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OrganizationRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override OrganizationRoleAssignmentReadDto MapToDto(OrganizationRoleAssignment entity)
    {
        return OrganizationRoleAssignmentMapper.ToDto(entity);
    }

    public override async Task<OrganizationRoleAssignment> ExecuteCreate(OrganizationRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("OrganizationRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<OrganizationRoleAssignment> ExecuteUpdate(OrganizationRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("OrganizationRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}