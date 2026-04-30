using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;

public class OutputSetRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OutputSetRepositorySettings> options, QuotaService quotaService)
{
    public OutputSetRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OutputSetRepository(dbContext, principalProvider, bus, options, quotaService);
    }
}

public class OutputSetRepository : GenericModuleChildRepository<
    OutputSet,
    OutputSetReadDto,
    OutputSetCreatedEvent,
    OutputSetUpdatedEvent,
    OutputSetDeletedEvent,
    OutputSetRepositorySettings>
{
    private readonly QuotaService _quotaService;

    public OutputSetRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OutputSetRepositorySettings> options,
        QuotaService quotaService)
        : base(dbContext, principalProvider, bus, options, quotaService)
    {
        _quotaService = quotaService;
    }

    protected override OutputSetReadDto MapToDto(OutputSet entity)
    {
        return OutputSetMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(OutputSet entity)
    {
        var currentCount = await DbContext.OutputSets
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.OutputSetQuota), currentCount);
    }

    public async Task<OutputSet> Get(Guid moduleId, string checksum, Guid organizationId)
    {
        var outputSet = await DbContext.OutputSets
            .Where(os => os.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Checksum == checksum && i.ModuleId == moduleId);

        if (outputSet == null)
            throw new EntityNotFoundException($"OutputSet with checksum \"{checksum}\" not found.");

        return outputSet;
    }

    public Task<OutputSet> GetLatestByModuleId(Guid moduleId, Guid organizationId)
    {
        var entity = DbContext.OutputSets
            .Include(m => m.Outputs)
            .Where(m => m.ModuleId == moduleId && m.OrganizationId == organizationId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();

        if (entity == null)
            throw new EntityNotFoundException($"Outputs for module {moduleId} not found");

        return Task.FromResult(entity);
    }

    /// <summary>
    /// Gets the latest OutputSet for a module, or null if none exists.
    /// Includes all outputs for comparison purposes.
    /// </summary>
    public Task<OutputSet?> GetLatestByModuleIdOrDefault(Guid moduleId, Guid organizationId)
    {
        var entity = DbContext.OutputSets
            .Include(m => m.Outputs)
            .Where(m => m.ModuleId == moduleId && m.OrganizationId == organizationId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();

        return Task.FromResult(entity);
    }

    public async Task<Guid?> CreateWithOutputs(OutputSet outputSet, Guid organizationId, bool inTransaction = false)
    {

        if (outputSet.Id == Guid.Empty)
            throw new IdIsEmptyException($"{typeof(OutputSet)} ID cannot be empty.");
        
        outputSet.OrganizationId = organizationId;
        
        // Set audit fields
        var principalId = PrincipalProvider.GetSubjectOrDefault(outputSet.OrganizationId);
        var principalDiscriminator = PrincipalProvider.GetPrincipalDiscriminatorOrDefault();
        var auditDiscriminator = ConvertToAuditPrincipalDiscriminator(principalDiscriminator);
        
        var now = DateTime.UtcNow;
        
        outputSet.CreatedBy = principalId;
        outputSet.CreatedByPrincipalDiscriminator = auditDiscriminator;
        outputSet.CreatedDateTime = now;
        outputSet.ModifiedBy = principalId;
        outputSet.ModifiedByPrincipalDiscriminator = auditDiscriminator;
        outputSet.ModifiedDateTime = now;
        
        
        foreach (var output in outputSet.Outputs)
        {
            
            if (output.Id == Guid.Empty)
                throw new IdIsEmptyException($"{typeof(Output)} ID cannot be empty.");
            
            output.OutputSetId = outputSet.Id;
            output.OrganizationId = organizationId;
                
            output.CreatedBy = principalId;
            output.CreatedByPrincipalDiscriminator = auditDiscriminator;
            output.CreatedDateTime = now;
            output.ModifiedBy = principalId;
            output.ModifiedByPrincipalDiscriminator = auditDiscriminator;
            output.ModifiedDateTime = now;
            
        }

        var mostRecentChecksum = DbContext.OutputSets
            .Where(m => m.ModuleId == outputSet.ModuleId && m.OrganizationId == organizationId)
            .OrderByDescending(m => m.Timestamp)
            .Select(m => m.Checksum)
            .FirstOrDefault();

        var exists = mostRecentChecksum != null && mostRecentChecksum == outputSet.Checksum;

        if (!exists)
        {
            // Check Output quotas before creating
            await CheckOutputQuotasAsync(outputSet, organizationId);

            if (inTransaction)
            {
                await CreateInTransaction(outputSet);
            }
            else
            {
                await ExecuteCreate(outputSet);
            }

            return outputSet.Id;
        }

        // Do nothing, we never update old outputs, just create new ones with new checksum
        return null;
    }

    private async Task CheckOutputQuotasAsync(OutputSet outputSet, Guid organizationId)
    {
        var outputCount = outputSet.Outputs.Count;

        // Check per-set quota
        var perSetQuota = await _quotaService.GetQuotaAsync(organizationId, nameof(Settings.QuotaLimits.OutputPerSetQuota));
        if (perSetQuota.HasValue && outputCount > perSetQuota.Value)
        {
            throw new QuotaExceededException(
                "Output",
                outputCount,
                perSetQuota.Value,
                $"Output per-set quota exceeded. This set contains {outputCount} outputs, limit is {perSetQuota.Value} per set.");
        }

        // Check org-level quota
        var outputQuota = await _quotaService.GetQuotaAsync(organizationId, nameof(Settings.QuotaLimits.OutputQuota));
        if (outputQuota.HasValue)
        {
            var currentOrgOutputCount = await DbContext.Set<Output>()
                .CountAsync(o => o.OrganizationId == organizationId);

            var newTotal = currentOrgOutputCount + outputCount;

            if (newTotal > outputQuota.Value)
            {
                throw new QuotaExceededException(
                    "Output",
                    currentOrgOutputCount,
                    outputQuota.Value,
                    $"Organization output quota exceeded. Current: {currentOrgOutputCount}, adding: {outputCount}, limit: {outputQuota.Value}.");
            }
        }
    }

    public Task<List<OutputSet>> ListSetsByIds(List<Guid> outputSetIds, Guid organizationId)
    {
        return Task.FromResult(DbContext.OutputSets
            .Where(x => outputSetIds.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList());
    }
}