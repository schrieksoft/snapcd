using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceInputs;
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

public class NamespaceInputFromSecretSecuredRepositoryFactory<TEntity>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceInputFromSecretRepositorySettings> options)
    where TEntity : NamespaceInputWithType, INamespaceInputFromSecret
{
    public NamespaceInputFromSecretSecuredRepository<TEntity> Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceInputFromSecretSecuredRepository<TEntity>(
            new NamespaceInputFromSecretRepository<TEntity>(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceInputFromSecretSecuredRepository<TEntity> : GenericNamespaceChildSecuredRepository<
    TEntity,
    NamespaceInputFromSecretReadDto,
    NamespaceInputFromSecretRepository<TEntity>,
    NamespaceInputFromSecretCreatedEvent,
    NamespaceInputFromSecretUpdatedEvent,
    NamespaceInputFromSecretDeletedEvent,
    NamespaceInputFromSecretRepositorySettings>
    where TEntity : NamespaceInputWithType, INamespaceInputFromSecret
{
    public NamespaceInputFromSecretSecuredRepository(
        NamespaceInputFromSecretRepository<TEntity> repository,
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

    public async Task<TEntity> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(namespaceId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to {typeof(TEntity).Name} {entity.Id}");

        return entity;
    }
}