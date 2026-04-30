using SnapCd.Server.Core.Dtos.OrganizationUsers;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class OrganizationUserMapper
{
    public static OrganizationUser ToEntity(OrganizationUserCreateDto dto, Guid organizationId)
    {
        return new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            JoinedAt = dto.JoinedAt,
            LastAccessedAt = dto.LastAccessedAt,
            IsDeactivated = dto.IsDeactivated,

            InvitationToken = dto.InvitationToken,
            InvitationSentDateTime = dto.InvitationSentDateTime,
            InvitationExpirationDateTime = dto.InvitationExpirationDateTime,
            InvitationCompleted = dto.InvitationCompleted,
            InvitationCompletedDateTime = dto.InvitationCompletedDateTime
        };
    }

    public static OrganizationUserReadDto ToDto(OrganizationUser entity)
    {
        return new OrganizationUserReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            JoinedAt = entity.JoinedAt,
            LastAccessedAt = entity.LastAccessedAt,
            IsDeactivated = entity.IsDeactivated,

            InvitationToken = entity.InvitationToken,
            InvitationSentDateTime = entity.InvitationSentDateTime,
            InvitationExpirationDateTime = entity.InvitationExpirationDateTime,
            InvitationCompleted = entity.InvitationCompleted,
            InvitationCompletedDateTime = entity.InvitationCompletedDateTime
        };
    }

    public static void UpdateEntity(OrganizationUser entity, OrganizationUserUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.JoinedAt = dto.JoinedAt;
        entity.LastAccessedAt = dto.LastAccessedAt;
        entity.IsDeactivated = dto.IsDeactivated;

        entity.InvitationToken = dto.InvitationToken;
        entity.InvitationSentDateTime = dto.InvitationSentDateTime;
        entity.InvitationExpirationDateTime = dto.InvitationExpirationDateTime;
        entity.InvitationCompleted = dto.InvitationCompleted;
        entity.InvitationCompletedDateTime = dto.InvitationCompletedDateTime;
    }
}