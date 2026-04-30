using System.Collections.Concurrent;
using MassTransit;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Services.Outputs;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class
    OutputSetService : GenericCrudService<OutputSet, OutputSetCreateDto, OutputSetUpdateDto, OutputSetReadDto, OutputSetSecuredRepository, OutputSetRepository, OutputSetCreatedEvent, OutputSetUpdatedEvent, OutputSetDeletedEvent,
    OutputSetRepositorySettings>
{
    private readonly CustomOutputMapper _outputMapper;

    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;
    private readonly IVaultFactory _vaultFactory;
    private readonly SecretOutputRepository _secretOutputRepository;
    private readonly ModuleSecuredRepository _moduleSecuredRepository;
    private readonly IOptions<OutputSetRepositorySettings> _repositorySettings;

    public OutputSetService(
        OutputSetSecuredRepository securedRepository,
        CustomOutputMapper outputMapper,
        SnapCdDbContext dbContext,
        IBus bus,
        IVaultFactory vaultFactory,
        SecretOutputRepository secretOutputRepository,
        ModuleSecuredRepository moduleSecuredRepository,
        IOptions<OutputSetRepositorySettings> repositorySettings) : base(securedRepository)
    {
        _outputMapper = outputMapper;
        _dbContext = dbContext;
        _bus = bus;
        _vaultFactory = vaultFactory;
        _secretOutputRepository = secretOutputRepository;
        _moduleSecuredRepository = moduleSecuredRepository;
        _repositorySettings = repositorySettings;
    }

    protected override OutputSet MapToEntity(OutputSetCreateDto dto, Guid organizationId)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    protected override OutputSetReadDto MapToDto(OutputSet entity)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    protected override void UpdateEntityFromDto(OutputSet entity, OutputSetUpdateDto dto)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    
    
    public async Task<Guid?> CreateWithOutputsNonsecured(OutputSetCreateDto outputSetDto, Guid moduleId, Guid organizationId)
    {
        var (outputSet, outputKeyVaultUrl) = _outputMapper.MapOutputSet(outputSetDto, moduleId, organizationId);

        // Fetch previous OutputSet for change detection before creating new one
        var previousOutputSet = await SecuredRepository.Repository.GetLatestByModuleIdOrDefault(moduleId, organizationId);

        await using var transaction = await SecuredRepository.Repository.DbContext.Database.BeginTransactionAsync();
        try
        {
            // Create OutputSet - events are disabled via appsettings
            var createdId = await SecuredRepository.Repository.CreateWithOutputs(outputSet, organizationId);

            if (createdId == null)
            {
                // OutputSet already exists with same checksum - no changes
                await transaction.CommitAsync();
                return null;
            }

            // Set vault secrets and track which ones changed
            Dictionary<string, bool> secretChanges;
            try
            {
                secretChanges = await SetRemoteSecretsAndTrackChanges(outputSet, outputKeyVaultUrl, outputSetDto);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set remote secrets. Error: {ex.Message}");
            }

            // Detect which outputs were created or updated
            var createdOrUpdatedOutputs = OutputChangeDetector.DetectChanges(previousOutputSet, outputSet, secretChanges);

            await transaction.CommitAsync();

            // Publish custom event with change information
            await PublishOutputSetWithOutputsCreatedEvent(outputSet, organizationId, createdOrUpdatedOutputs);

            return createdId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    

    public async Task<Guid?> CreateWithOutputs(OutputSetCreateDto outputSetDto, Guid moduleId, Guid organizationId)
    {
        var (outputSet, outputKeyVaultUrl) = _outputMapper.MapOutputSet(outputSetDto, moduleId, organizationId);

        // Fetch previous OutputSet for change detection before creating new one
        var previousOutputSet = await SecuredRepository.Repository.GetLatestByModuleIdOrDefault(moduleId, organizationId);

        await using var transaction = await SecuredRepository.Repository.DbContext.Database.BeginTransactionAsync();
        try
        {
            // Create OutputSet - events are disabled via appsettings
            var createdId = await SecuredRepository.CreateWithOutputs(outputSet, organizationId);

            if (createdId == null)
            {
                // OutputSet already exists with same checksum - no changes
                await transaction.CommitAsync();
                return null;
            }

            // Set vault secrets and track which ones changed
            Dictionary<string, bool> secretChanges;
            try
            {
                secretChanges = await SetRemoteSecretsAndTrackChanges(outputSet, outputKeyVaultUrl, outputSetDto);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set remote secrets. Error: {ex.Message}");
            }

            // Detect which outputs were created or updated
            var createdOrUpdatedOutputs = OutputChangeDetector.DetectChanges(previousOutputSet, outputSet, secretChanges);

            await transaction.CommitAsync();

            // Publish custom event with change information
            await PublishOutputSetWithOutputsCreatedEvent(outputSet, organizationId, createdOrUpdatedOutputs);

            return createdId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    /// <summary>
    /// Sets remote secrets in the vault and tracks which ones were changed.
    /// </summary>
    /// <returns>Dictionary mapping output names to whether they were changed in the vault.</returns>
    private async Task<Dictionary<string, bool>> SetRemoteSecretsAndTrackChanges(
        OutputSet outputSet,
        string outputKeyVaultUrl,
        OutputSetCreateDto outputSetDto)
    {
        var secretChanges = new ConcurrentDictionary<string, bool>();

        var tasks = outputSet.Outputs
            .OfType<SecretOutput>()
            .Select(async outputSecret =>
            {
                var outputDto = (outputSetDto.Outputs ?? []).First(x => x.Name == outputSecret.Name);

                using var vault = _vaultFactory.Create(outputKeyVaultUrl);
                var result = await vault.SetIfChanged(outputSecret.RemoteSecretName, outputDto.Value);
                secretChanges[outputSecret.Name] = result.WasChanged;
            });

        await Task.WhenAll(tasks);

        return new Dictionary<string, bool>(secretChanges);
    }

    /// <summary>
    /// Publishes the OutputSetWithOutputsCreatedEvent with change information.
    /// </summary>
    private async Task PublishOutputSetWithOutputsCreatedEvent(
        OutputSet outputSet,
        Guid organizationId,
        List<string> createdOrUpdatedOutputs)
    {
        var eventDto = new OutputSetWithOutputsDto()
        {
            Id = outputSet.Id,
            ModuleId = outputSet.ModuleId,
            CreatedOrUpdatedOutputs = createdOrUpdatedOutputs
        };

        var createdEvent = new OutputSetWithOutputsCreatedEvent
        {
            Data = eventDto,
            OrganizationId = organizationId,
            CreatedBy = outputSet.CreatedBy,
            CreatedByPrincipalDiscriminator = outputSet.CreatedByPrincipalDiscriminator,
            CreatedDateTime = outputSet.CreatedDateTime,
            ModifiedBy = outputSet.ModifiedBy,
            ModifiedByPrincipalDiscriminator = outputSet.ModifiedByPrincipalDiscriminator,
            ModifiedDateTime = outputSet.ModifiedDateTime
        };

        await _bus.Publish(createdEvent,
            publishContext => { publishContext.TimeToLive = _repositorySettings.Value.EventTtl; });
    }

    public override void Dispose()
    {
        base.Dispose();
        _moduleSecuredRepository?.Dispose();
    }
}