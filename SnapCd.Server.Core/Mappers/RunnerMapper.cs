using SnapCd.Contracts.Dto.Runners;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class RunnerMapper
{
    public static Runner ToEntity(RunnerCreateDto dto, Guid organizationId)
    {
        return new Runner
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            Name = dto.Name,
            IsDisabled = dto.IsDisabled,
            AllowMultipleInstances = dto.AllowMultipleInstances,
            IsAssignedToAllModules = dto.IsAssignedToAllModules
        };
    }

    public static RunnerReadDto ToDto(Runner entity)
    {
        return new RunnerReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            Name = entity.Name,
            IsDisabled = entity.IsDisabled,
            AllowMultipleInstances = entity.AllowMultipleInstances,
            IsAssignedToAllModules = entity.IsAssignedToAllModules
        };
    }

    public static void UpdateEntity(Runner entity, RunnerUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.Name = dto.Name;
        entity.IsDisabled = dto.IsDisabled;
        entity.AllowMultipleInstances = dto.AllowMultipleInstances;
        entity.IsAssignedToAllModules = dto.IsAssignedToAllModules;
    }
}