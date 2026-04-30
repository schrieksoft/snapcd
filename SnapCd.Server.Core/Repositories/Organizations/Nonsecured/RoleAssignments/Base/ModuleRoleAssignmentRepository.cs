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

public class ModuleRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleRoleAssignmentRepositorySettings> options)
{
    public ModuleRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleRoleAssignmentRepository : GenericModuleChildRepository<ModuleRoleAssignment, ModuleRoleAssignmentReadDto, ModuleRoleAssignmentCreatedEvent, ModuleRoleAssignmentUpdatedEvent,
    ModuleRoleAssignmentDeletedEvent, ModuleRoleAssignmentRepositorySettings>
{
    public ModuleRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleRoleAssignmentReadDto MapToDto(ModuleRoleAssignment entity)
    {
        return ModuleRoleAssignmentMapper.ToDto(entity);
    }

    public override async Task<ModuleRoleAssignment> ExecuteCreate(ModuleRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("ModuleRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<ModuleRoleAssignment> ExecuteUpdate(ModuleRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("ModuleRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}