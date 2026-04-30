using SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespacePulumiArrayFlagMapper
{
    public static NamespacePulumiArrayFlag ToEntity(NamespacePulumiArrayFlagCreateDto dto, Guid organizationId)
    {
        return new NamespacePulumiArrayFlag
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Flag = dto.Flag,
            Value = dto.Value,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespacePulumiArrayFlagReadDto ToDto(NamespacePulumiArrayFlag entity)
    {
        return new NamespacePulumiArrayFlagReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Flag = entity.Flag,
            Value = entity.Value,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespacePulumiArrayFlag entity, NamespacePulumiArrayFlagUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Flag = dto.Flag;
        entity.Value = dto.Value;
        entity.NamespaceId = dto.NamespaceId;
    }
}
