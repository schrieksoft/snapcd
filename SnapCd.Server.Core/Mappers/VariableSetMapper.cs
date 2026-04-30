using MassTransit;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class VariableSetMapper
{
    public static VariableSet ToEntity(VariableSetCreateDto dto, Guid organizationId)
    {
        throw new NotImplementedByDesignException();
    }

    public static VariableSetReadDto ToDto(VariableSet entity)
    {
        return new VariableSetReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Timestamp = entity.Timestamp,
            Checksum = entity.Checksum
        };
    }

    public static void UpdateEntity(VariableSet entity, VariableSetUpdateDto dto)
    {
        throw new NotImplementedByDesignException();
    }
}