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

public class NamespaceRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceRoleAssignmentRepositorySettings> options)
{
    public NamespaceRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceRoleAssignmentRepository : GenericNamespaceChildRepository<NamespaceRoleAssignment, NamespaceRoleAssignmentReadDto, NamespaceRoleAssignmentCreatedEvent,
    NamespaceRoleAssignmentUpdatedEvent, NamespaceRoleAssignmentDeletedEvent, NamespaceRoleAssignmentRepositorySettings>
{
    public NamespaceRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceRoleAssignmentReadDto MapToDto(NamespaceRoleAssignment entity)
    {
        return NamespaceRoleAssignmentMapper.ToDto(entity);
    }

    public override async Task<NamespaceRoleAssignment> ExecuteCreate(NamespaceRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("NamespaceRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<NamespaceRoleAssignment> ExecuteUpdate(NamespaceRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("NamespaceRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}