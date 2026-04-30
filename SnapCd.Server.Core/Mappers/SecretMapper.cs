using SnapCd.Contracts.Dto.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets;

namespace SnapCd.Server.Core.Mappers;

public static class SecretMapper
{
    public static Secret ToEntity(SecretCreateDto dto, Guid organizationId)
    {
        return new Secret
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name
        };
    }

    public static SecretDto ToDto(Secret entity)
    {
        return new SecretDto
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }

    public static void UpdateEntity(Secret entity, SecretUpdateDto dto)
    {
        entity.Name = dto.Name;
    }
}