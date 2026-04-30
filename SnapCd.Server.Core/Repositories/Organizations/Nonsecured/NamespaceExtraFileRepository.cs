using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceExtraFiles;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceExtraFileRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceExtraFileRepositorySettings> options)
{
    public NamespaceExtraFileRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceExtraFileRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceExtraFileRepository : GenericNamespaceChildDefinitionRepository<
    NamespaceExtraFile,
    NamespaceExtraFileReadDto,
    NamespaceExtraFileCreatedEvent,
    NamespaceExtraFileUpdatedEvent,
    NamespaceExtraFileDeletedEvent,
    NamespaceExtraFileRepositorySettings>
{
    public NamespaceExtraFileRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceExtraFileRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceExtraFileReadDto MapToDto(NamespaceExtraFile entity)
    {
        return NamespaceExtraFileMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(NamespaceExtraFile entity)
    {
        var currentCount = await DbContext.NamespaceExtraFiles
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespaceExtraFileQuota), currentCount);
    }

    public async Task<NamespaceExtraFile> Get(Guid namespaceId, string fileName, Guid organizationId)
    {
        var entity = await DbContext.NamespaceExtraFiles
            .Where(i => i.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.FileName == fileName && i.NamespaceId == namespaceId);

        if (entity == null)
            throw new EntityNotFoundException($"NamespaceExtraFile with fileName {fileName} not found.");

        return entity;
    }
}