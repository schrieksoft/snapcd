using MassTransit;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers.Outputs;

public static class OutputSetMapper
{
    public static OutputSet ToEntity(OutputSetCreateDto dto, Guid organizationId)
    {

        throw new NotImplementedByDesignException();
    }

    public static OutputSetReadDto ToDto(OutputSet entity)
    {
        var outputs = new List<OutputReadDto>();

        foreach (var output in entity.Outputs)
        {
            outputs.Add(OutputMapper.ToDto(output));
        }
        
        return new OutputSetReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Timestamp = entity.Timestamp,
            Checksum = entity.Checksum,
            Outputs =  outputs
        };
    }

    public static void UpdateEntity(OutputSet entity, OutputSetUpdateDto dto)
    {

        throw new NotImplementedByDesignException();
    }
}