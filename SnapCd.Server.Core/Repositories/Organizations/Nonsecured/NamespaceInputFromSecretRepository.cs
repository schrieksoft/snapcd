using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceInputFromSecretRepository<TEntity> : GenericNamespaceChildDefinitionRepository<
    TEntity,
    NamespaceInputFromSecretReadDto,
    NamespaceInputFromSecretCreatedEvent,
    NamespaceInputFromSecretUpdatedEvent,
    NamespaceInputFromSecretDeletedEvent,
    NamespaceInputFromSecretRepositorySettings>
    where TEntity : NamespaceInputWithType, INamespaceInputFromSecret
{
    public NamespaceInputFromSecretRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceInputFromSecretRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceInputFromSecretReadDto MapToDto(TEntity entity)
    {
        return NamespaceInputFromSecretMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(TEntity entity)
    {
        var currentCount = await DbContext.Set<TEntity>()
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        // Determine quota name based on entity type (Param or EnvVar)
        var typeName = typeof(TEntity).Name;
        var quotaName = typeName.Contains("Param")
            ? nameof(Settings.QuotaLimits.NamespaceParamFromSecretQuota)
            : nameof(Settings.QuotaLimits.NamespaceEnvVarFromSecretQuota);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, quotaName, currentCount);
    }

    public async Task<TEntity> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await DbContext.Set<TEntity>()
            .Where(i => i.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Name == name && i.NamespaceId == namespaceId);

        if (entity == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with name {name} not found.");

        return entity;
    }
}