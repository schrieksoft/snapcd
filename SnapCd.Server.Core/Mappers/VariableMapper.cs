using MassTransit;
using SnapCd.Contracts.Dto.Variables;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class VariableMapper
{
    public static Variable ToEntity(VariableCreateDto dto, Guid organizationId)
    {

        throw new NotImplementedByDesignException();
    }

    public static VariableReadDto ToDto(Variable entity)
    {
        return new VariableReadDto
        {
            Id = entity.Id,
            VariableSetId = entity.VariableSetId,
            Name = entity.Name,
            Type = entity.Type,
            Description = entity.Description,
            Sensitive = entity.Sensitive,
            Nullable = entity.Nullable,
            FromExtraFile = entity.FromExtraFile
        };
    }

    public static void UpdateEntity(Variable entity, VariableUpdateDto dto)
    {

        throw new NotImplementedByDesignException();
    }
}