using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

namespace SnapCd.Server.Core.Mappers;

public static class StackSecretMapper
{
    public static StackSecret ToEntity(StackSecretDto dto, Guid organizationId)
    {
        return new StackSecret
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            StackId = dto.StackId
        };
    }

    public static StackSecretDto ToDto(StackSecret entity)
    {
        return new StackSecretDto
        {
            Id = entity.Id,
            Name = entity.Name,
            StackId = entity.StackId
        };
    }

    public static void UpdateEntity(StackSecret entity, StackSecretDto dto)
    {
        entity.Name = dto.Name;
        entity.StackId = dto.StackId;
    }
}