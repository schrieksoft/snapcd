using SnapCd.Server.Core.Dtos.Organizations;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class OrganizationMapper
{
    public static OrganizationReadDto ToDto(Organization entity)
    {
        return new OrganizationReadDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = null, // Organization doesn't have Description in the entity
            CreatedDateTime = entity.CreatedDateTime,
            DeletedDateTime = entity.DeletedDateTime,
            DeletedByUserId = entity.DeletedByUserId
        };
    }
}