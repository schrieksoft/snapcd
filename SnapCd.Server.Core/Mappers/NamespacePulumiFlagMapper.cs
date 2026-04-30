using SnapCd.Contracts.Dto.NamespacePulumiFlags;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespacePulumiFlagMapper
{
    public static NamespacePulumiFlag ToEntity(NamespacePulumiFlagCreateDto dto, Guid organizationId)
    {
        return new NamespacePulumiFlag
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Flag = dto.Flag,
            Value = dto.Value,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespacePulumiFlagReadDto ToDto(NamespacePulumiFlag entity)
    {
        return new NamespacePulumiFlagReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Flag = entity.Flag,
            Value = entity.Value,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespacePulumiFlag entity, NamespacePulumiFlagUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Flag = dto.Flag;
        entity.Value = dto.Value;
        entity.NamespaceId = dto.NamespaceId;
    }
}
