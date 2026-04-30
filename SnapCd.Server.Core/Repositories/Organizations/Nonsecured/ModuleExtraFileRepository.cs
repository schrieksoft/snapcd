using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleExtraFiles;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleExtraFileRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleExtraFileRepositorySettings> options)
{
    public ModuleExtraFileRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleExtraFileRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleExtraFileRepository : GenericModuleChildDefinitionRepository<ModuleExtraFile, ModuleExtraFileReadDto, ModuleExtraFileCreatedEvent, ModuleExtraFileUpdatedEvent, ModuleExtraFileDeletedEvent,
    ModuleExtraFileRepositorySettings>
{
    public ModuleExtraFileRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleExtraFileRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleExtraFileReadDto MapToDto(ModuleExtraFile entity)
    {
        return ModuleExtraFileMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleExtraFile entity)
    {
        var currentCount = await DbContext.ModuleExtraFiles
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleExtraFileQuota), currentCount);
    }

    public async Task<ModuleExtraFile> Get(Guid moduleId, string fileName, Guid organizationId)
    {
        var entity = await DbContext.ModuleExtraFiles
            .SingleOrDefaultAsync(e => e.FileName == fileName && e.ModuleId == moduleId && e.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"ModuleExtraFile with name {fileName} not found for module {moduleId}.");

        return entity;
    }
}