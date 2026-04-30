using SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceTerraformArrayFlagMapper
{
    public static NamespaceTerraformArrayFlag ToEntity(NamespaceTerraformArrayFlagCreateDto dto, Guid organizationId)
    {
        return new NamespaceTerraformArrayFlag
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Flag = dto.Flag,
            Value = dto.Value,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespaceTerraformArrayFlagReadDto ToDto(NamespaceTerraformArrayFlag entity)
    {
        return new NamespaceTerraformArrayFlagReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Flag = entity.Flag,
            Value = entity.Value,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespaceTerraformArrayFlag entity, NamespaceTerraformArrayFlagUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Flag = dto.Flag;
        entity.Value = dto.Value;
        entity.NamespaceId = dto.NamespaceId;
    }
}
