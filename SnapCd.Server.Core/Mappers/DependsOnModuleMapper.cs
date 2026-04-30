using SnapCd.Contracts.Dto.DependsOnModules;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class DependsOnModuleMapper
{
    public static DependsOnModule ToEntity(DependsOnModuleCreateDto dto, Guid organizationId)
    {
        return new DependsOnModule
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            DependsOnModuleId = dto.DependsOnModuleId
        };
    }

    public static DependsOnModuleReadDto ToDto(DependsOnModule entity)
    {
        return new DependsOnModuleReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            DependsOnModuleId = entity.DependsOnModuleId
        };
    }

    public static void UpdateEntity(DependsOnModule entity, DependsOnModuleUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.DependsOnModuleId = dto.DependsOnModuleId;
    }
}