using SnapCd.Contracts.Dto.NamespaceTerraformFlags;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceTerraformFlagMapper
{
    public static NamespaceTerraformFlag ToEntity(NamespaceTerraformFlagCreateDto dto, Guid organizationId)
    {
        return new NamespaceTerraformFlag
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Flag = dto.Flag,
            Value = dto.Value,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespaceTerraformFlagReadDto ToDto(NamespaceTerraformFlag entity)
    {
        return new NamespaceTerraformFlagReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Flag = entity.Flag,
            Value = entity.Value,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespaceTerraformFlag entity, NamespaceTerraformFlagUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Flag = dto.Flag;
        entity.Value = dto.Value;
        entity.NamespaceId = dto.NamespaceId;
    }
}
