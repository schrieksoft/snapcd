using SnapCd.Contracts.Dto.ModuleJobApprovals;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleJobApprovalMapper
{
    public static ModuleJobApproval ToEntity(ModuleJobApprovalCreateDto dto, Guid organizationId)
    {
        return new ModuleJobApproval
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleJobId = dto.ModuleJobId,
            PrincipalId = dto.PrincipalId,
            PrincipalDiscriminator = dto.PrincipalDiscriminator,
            DecisionDateTime = dto.DecisionDateTime,
            Declined = dto.Declined
        };
    }

    public static ModuleJobApprovalReadDto ToDto(ModuleJobApproval entity)
    {
        return new ModuleJobApprovalReadDto
        {
            Id = entity.Id,
            ModuleJobId = entity.ModuleJobId,
            PrincipalId = entity.PrincipalId,
            PrincipalDiscriminator = entity.PrincipalDiscriminator,
            DecisionDateTime = entity.DecisionDateTime,
            Declined = entity.Declined
        };
    }

    public static void UpdateEntity(ModuleJobApproval entity, ModuleJobApprovalUpdateDto dto)
    {
        entity.ModuleJobId = dto.ModuleJobId;
        entity.PrincipalId = dto.PrincipalId;
        entity.PrincipalDiscriminator = dto.PrincipalDiscriminator;
        entity.DecisionDateTime = dto.DecisionDateTime;
        entity.Declined = dto.Declined;
    }
}