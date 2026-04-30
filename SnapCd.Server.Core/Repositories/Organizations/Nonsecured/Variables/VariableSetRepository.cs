using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;

public class VariableSetRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<VariableSetRepositorySettings> options, QuotaService quotaService)
{
    public VariableSetRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new VariableSetRepository(dbContext, principalProvider, bus, options, quotaService);
    }
}

public class VariableSetRepository : GenericModuleChildRepository<
    VariableSet,
    VariableSetReadDto,
    VariableSetCreatedEvent,
    VariableSetUpdatedEvent,
    VariableSetDeletedEvent,
    VariableSetRepositorySettings>
{
    private readonly QuotaService _quotaService;

    public VariableSetRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<VariableSetRepositorySettings> options,
        QuotaService quotaService)
        : base(dbContext, principalProvider, bus, options, quotaService)
    {
        _quotaService = quotaService;
    }

    protected override VariableSetReadDto MapToDto(VariableSet entity)
    {
        return VariableSetMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(VariableSet entity)
    {
        var currentCount = await DbContext.VariableSets
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.VariableSetQuota), currentCount);
    }

    public async Task<VariableSet> Get(Guid moduleId, string checksum, Guid organizationId)
    {
        var variableSet = await DbContext.VariableSets
            .Where(ins => ins.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Checksum == checksum && i.ModuleId == moduleId);

        if (variableSet == null)
            throw new EntityNotFoundException($"VariableSet with checksum \"{checksum}\" not found.");

        return variableSet;
    }

    public Task<VariableSet> GetLatestByModuleId(Guid moduleId, Guid organizationId)
    {
        var entity = DbContext.VariableSets
            .Include(m => m.Variables)
            .Where(m => m.ModuleId == moduleId && m.OrganizationId == organizationId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();

        if (entity == null)
            throw new EntityNotFoundException($"Inputs for module {moduleId} not found");

        return Task.FromResult(entity);
    }

    public async Task<Guid?> CreateWithVariables(VariableSet variableSet, Guid organizationId)
    {
        if (variableSet.Id == Guid.Empty)
            throw new IdIsEmptyException($"{typeof(VariableSet)} ID cannot be empty.");
        
        variableSet.OrganizationId = organizationId;
        
        var principalId = PrincipalProvider.GetSubjectOrDefault(variableSet.OrganizationId);
        var principalDiscriminator = PrincipalProvider.GetPrincipalDiscriminatorOrDefault();
        var auditDiscriminator = ConvertToAuditPrincipalDiscriminator(principalDiscriminator);

        // Set audit fields

        var now = DateTime.UtcNow;
        
        variableSet.CreatedBy = principalId;
        variableSet.CreatedByPrincipalDiscriminator = auditDiscriminator;
        variableSet.CreatedDateTime = now;
        variableSet.ModifiedBy = principalId;
        variableSet.ModifiedByPrincipalDiscriminator = auditDiscriminator;
        variableSet.ModifiedDateTime = now;
        

        foreach (var variable in variableSet.Variables)
        {
            if (variable.Id == Guid.Empty)
                throw new IdIsEmptyException($"{typeof(Variable)} ID cannot be empty.");
            
            variable.VariableSetId = variableSet.Id;
            variable.OrganizationId = organizationId;
            variable.CreatedBy = principalId;
            variable.CreatedByPrincipalDiscriminator = auditDiscriminator;
            variable.CreatedDateTime = now;
            variable.ModifiedBy = principalId;
            variable.ModifiedByPrincipalDiscriminator = auditDiscriminator;
            variable.ModifiedDateTime = now;
        }

        var mostRecentChecksum = DbContext.VariableSets
            .Where(m => m.ModuleId == variableSet.ModuleId && m.OrganizationId == organizationId)
            .OrderByDescending(m => m.Timestamp)
            .Select(m => m.Checksum)
            .FirstOrDefault();

        var exists = mostRecentChecksum != null && mostRecentChecksum == variableSet.Checksum;

        if (!exists)
        {
            // Check Variable quotas before creating
            await CheckVariableQuotasAsync(variableSet, organizationId);

            DbContext.VariableSets.Add(variableSet);
            await DbContext.SaveChangesAsync();
            return variableSet.Id;
        }

        // Do nothing, we never update old inputs, just create new ones with new checksum
        return null;
    }

    private async Task CheckVariableQuotasAsync(VariableSet variableSet, Guid organizationId)
    {
        var variableCount = variableSet.Variables.Count;

        // Check per-set quota
        var perSetQuota = await _quotaService.GetQuotaAsync(organizationId, nameof(Settings.QuotaLimits.VariablePerSetQuota));
        if (perSetQuota.HasValue && variableCount > perSetQuota.Value)
        {
            throw new QuotaExceededException(
                "Variable",
                variableCount,
                perSetQuota.Value,
                $"Variable per-set quota exceeded. This set contains {variableCount} variables, limit is {perSetQuota.Value} per set.");
        }

        // Check org-level quota
        var variableQuota = await _quotaService.GetQuotaAsync(organizationId, nameof(Settings.QuotaLimits.VariableQuota));
        if (variableQuota.HasValue)
        {
            var currentOrgVariableCount = await DbContext.Set<Variable>()
                .CountAsync(v => v.OrganizationId == organizationId);

            var newTotal = currentOrgVariableCount + variableCount;

            if (newTotal > variableQuota.Value)
            {
                throw new QuotaExceededException(
                    "Variable",
                    currentOrgVariableCount,
                    variableQuota.Value,
                    $"Organization variable quota exceeded. Current: {currentOrgVariableCount}, adding: {variableCount}, limit: {variableQuota.Value}.");
            }
        }
    }

    public Task<List<VariableSet>> ListSetsByIds(List<Guid> variableSetIds, Guid organizationId)
    {
        return Task.FromResult(DbContext.VariableSets
            .Where(x => variableSetIds.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList());
    }
}