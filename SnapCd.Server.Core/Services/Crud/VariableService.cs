using MassTransit;
using SnapCd.Contracts.Dto.Variables;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Variables;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class InputServiceFactory(VariableSecuredRepositoryFactory securedRepositoryFactory)
{
    public VariableService Create()
    {
        return new VariableService(securedRepositoryFactory.Create());
    }
}

public class VariableService : GenericCrudService<
    Variable,
    VariableCreateDto,
    VariableUpdateDto,
    VariableReadDto,
    VariableSecuredRepository,
    VariableRepository,
    InputCreatedEvent,
    InputUpdatedEvent,
    InputDeletedEvent,
    VariableRepositorySettings>
{
    public VariableService(VariableSecuredRepository securedRepository)
        : base(securedRepository)
    {
    }

    protected override Variable MapToEntity(VariableCreateDto dto, Guid organizationId)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    protected override VariableReadDto MapToDto(Variable entity)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    protected override void UpdateEntityFromDto(Variable entity, VariableUpdateDto dto)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    public async Task<List<VariableReadDto>> ListByVariableSetIds(List<Guid> variableSetIds, Guid organizationId)
    {
        if (variableSetIds == null || variableSetIds.Count == 0)
            return new List<VariableReadDto>();

        var inputs = await SecuredRepository.ListByVariableSetIds(variableSetIds, organizationId);
        return inputs.Select(MapToDto).ToList();
    }

    public async Task<List<VariableReadDto>> ListByIds(List<Guid> inputIds, Guid organizationId)
    {
        if (inputIds == null || inputIds.Count == 0)
            return new List<VariableReadDto>();

        var inputs = await SecuredRepository.ListByIds(inputIds, organizationId);
        return inputs.Select(MapToDto).ToList();
    }
}