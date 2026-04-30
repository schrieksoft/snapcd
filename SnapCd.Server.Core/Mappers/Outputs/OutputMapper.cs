using MassTransit;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers.Outputs;

public static class OutputMapper
{
    public static Output ToEntity(OutputCreateDto dto, Guid organizationId)
    {
        throw new NotImplementedByDesignException();
    }

    public static OutputReadDto ToDto(Output entity)
    {
        return new OutputReadDto
        {
            Id = entity.Id,
            OutputSetId = entity.OutputSetId,
            Name = entity.Name,
            Type = entity.Type,
        };
    }

    public static void UpdateEntity(Output entity, OutputUpdateDto dto)
    {

        throw new NotImplementedByDesignException();
    }
}