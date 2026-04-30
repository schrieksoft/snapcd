using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleInputFromSecretSecuredRepositoryFactory<TEntity>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleInputFromSecretRepositorySettings> options)
    where TEntity : ModuleInputWithType, IModuleInputFromSecret
{
    public ModuleInputFromSecretSecuredRepository<TEntity> Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleInputFromSecretSecuredRepository<TEntity>(
            new ModuleInputFromSecretRepository<TEntity>(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleInputFromSecretSecuredRepository<TEntity> : GenericModuleChildSecuredRepository<
    TEntity,
    ModuleInputFromSecretReadDto,
    ModuleInputFromSecretRepository<TEntity>,
    ModuleInputFromSecretCreatedEvent,
    ModuleInputFromSecretUpdatedEvent,
    ModuleInputFromSecretDeletedEvent,
    ModuleInputFromSecretRepositorySettings>
    where TEntity : ModuleInputWithType, IModuleInputFromSecret
{
    public ModuleInputFromSecretSecuredRepository(
        ModuleInputFromSecretRepository<TEntity> repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = base.ReadPermissionMap.OrganizationRoles,
        StackRoles = base.ReadPermissionMap.StackRoles,
        NamespaceRoles = base.ReadPermissionMap.NamespaceRoles,
        ModuleRoles = base.ReadPermissionMap.ModuleRoles
    };

    public async Task<TEntity> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to {typeof(TEntity).Name} {entity.Id}");

        return entity;
    }
}