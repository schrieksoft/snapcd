using MassTransit;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Variables;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class VariableSetServiceFactory(VariableSetSecuredRepositoryFactory securedRepositoryFactory)
{
    public VariableSetService Create()
    {
        return new VariableSetService(securedRepositoryFactory.Create());
    }
}

public class VariableSetService : GenericCrudService<
    VariableSet,
    VariableSetCreateDto,
    VariableSetUpdateDto,
    VariableSetReadDto,
    VariableSetSecuredRepository,
    VariableSetRepository,
    VariableSetCreatedEvent,
    VariableSetUpdatedEvent,
    VariableSetDeletedEvent,
    VariableSetRepositorySettings>
{
    public VariableSetService(VariableSetSecuredRepository securedRepository)
        : base(securedRepository)
    {
    }

    protected override VariableSet MapToEntity(VariableSetCreateDto dto, Guid organizationId)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    protected override VariableSetReadDto MapToDto(VariableSet entity)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    protected override void UpdateEntityFromDto(VariableSet entity, VariableSetUpdateDto dto)
    {
        throw new NotImplementedByDesignException("Variable mapping is performed directly in RunnerHub handler.");
    }

    public async Task<Guid?> CreateWithVariables(VariableSetCreateDto variableSetDto, Guid moduleId, Guid organizationId)
    {
        var variableSetId = Guid.NewGuid();

        var variableSet = new VariableSet
        {
            Id = variableSetId,
            OrganizationId = organizationId,
            ModuleId = moduleId,
            Timestamp = variableSetDto.Timestamp,
            Checksum = variableSetDto.Checksum,
            Variables = (variableSetDto.Variables ?? []).Select(inputDto => new Variable
            {
                Id = Guid.NewGuid(),
                VariableSetId = variableSetId,
                OrganizationId = organizationId,
                Name = inputDto.Name,
                Type = inputDto.Type,
                Description = inputDto.Description,
                Sensitive = inputDto.Sensitive,
                Nullable = inputDto.Nullable,
                FromExtraFile = inputDto.FromExtraFile
            }).ToList()
        };

        var createdId = await SecuredRepository.Repository.CreateWithVariables(variableSet, organizationId);

        // If createdId is null, then no new VariableSet was created (duplicate)
        return createdId;
    }

    public async Task<VariableSet> Get(Guid moduleId, string checksum, Guid organizationId)
    {
        return await SecuredRepository.Get(moduleId, checksum, organizationId);
    }

    public async Task<VariableSet> GetLatestByModuleId(Guid moduleId, Guid organizationId)
    {
        return await SecuredRepository.GetLatestByModuleId(moduleId, organizationId);
    }

    public async Task<List<VariableSet>> ListSetsByIds(List<Guid> variableSetIds, Guid organizationId)
    {
        return await SecuredRepository.ListSetsByIds(variableSetIds, organizationId);
    }
}