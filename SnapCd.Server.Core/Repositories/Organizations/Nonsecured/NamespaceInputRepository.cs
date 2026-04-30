using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceInputs.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceInputRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceInputRepositorySettings> options)
{
    public NamespaceInputRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceInputRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceInputRepository : GenericNamespaceChildDefinitionRepository<
    NamespaceInput,
    NamespaceInputReadDto,
    NamespaceInputCreatedEvent,
    NamespaceInputUpdatedEvent,
    NamespaceInputDeletedEvent,
    NamespaceInputRepositorySettings>
{
    public NamespaceInputRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceInputRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceInputReadDto MapToDto(NamespaceInput entity)
    {
        return NamespaceInputMapper.ToDto(entity);
    }

    public async Task<NamespaceInput> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await DbContext.NamespaceInputs
            .Where(i => i.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Name == name && i.NamespaceId == namespaceId);

        if (entity == null)
            throw new EntityNotFoundException($"{nameof(NamespaceInput)} with name {name} not found.");

        return entity;
    }
}