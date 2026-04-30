using MassTransit;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class OutputServiceFactory(
    OutputSecuredRepositoryFactory securedRepositoryFactory,
    CustomOutputMapper outputMapper,
    SnapCdDbContext dbContext)
{
    public OutputService Create()
    {
        return new OutputService(securedRepositoryFactory.Create(), outputMapper, dbContext);
    }
}

public class OutputService : GenericCrudService<Output, OutputCreateDto, OutputUpdateDto, OutputReadDto, OutputSecuredRepository, OutputRepository, OutputCreatedEvent, OutputUpdatedEvent, OutputDeletedEvent, OutputRepositorySettings>
{
    private readonly CustomOutputMapper _outputMapper;
    private readonly SnapCdDbContext _dbContext;

    public OutputService(
        OutputSecuredRepository securedRepository,
        CustomOutputMapper outputMapper,
        SnapCdDbContext dbContext
    ) : base(securedRepository)
    {
        _outputMapper = outputMapper;
        _dbContext = dbContext;
    }

    protected override Output MapToEntity(OutputCreateDto dto, Guid organizationId)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    protected override OutputReadDto MapToDto(Output entity)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    protected override void UpdateEntityFromDto(Output entity, OutputUpdateDto dto)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    public async Task<List<OutputReadDto>> ListByOutputSetIds(List<Guid> outputSetIds, Guid organizationId)
    {
        if (outputSetIds == null || outputSetIds.Count == 0)
            return new List<OutputReadDto>();

        var outputs = await SecuredRepository.ListByOutputSetIds(outputSetIds, organizationId);
        return await _outputMapper.MapOutputs(outputs, organizationId);
    }

    public async Task<List<OutputReadDto>> ListByIds(List<Guid> outputIds, Guid organizationId)
    {
        if (outputIds == null || outputIds.Count == 0)
            return new List<OutputReadDto>();

        var outputs = await SecuredRepository.ListByIds(outputIds, organizationId);
        return await _outputMapper.MapOutputs(outputs, organizationId);
    }

    public override async Task<OutputReadDto> Get(Guid id, Guid organizationId)
    {
        var output = await SecuredRepository.Get(id, organizationId);

        var organization = _outputMapper.GetOrganization(organizationId);

        return await _outputMapper.MapOutput(output, organizationId);
    }
}