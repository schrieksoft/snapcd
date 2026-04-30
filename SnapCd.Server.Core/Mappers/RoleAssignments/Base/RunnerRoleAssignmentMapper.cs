using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;

namespace SnapCd.Server.Core.Mappers.RoleAssignments.Base;

public static class RunnerRoleAssignmentMapper
{
    public static RunnerRoleAssignment ToEntity(RunnerRoleAssignmentCreateDto dto, Guid organizationId)
    {
        var id = Guid.NewGuid();

        return dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => new UserRunnerRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                RunnerId = dto.RunnerId,
                UserId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => new ServicePrincipalRunnerRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                RunnerId = dto.RunnerId,
                ServicePrincipalId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.Group => new GroupRunnerRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                RunnerId = dto.RunnerId,
                GroupId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };
    }

    public static RunnerRoleAssignmentReadDto ToDto(RunnerRoleAssignment entity)
    {
        return new RunnerRoleAssignmentReadDto
        {
            Id = entity.Id,
            RunnerId = entity.RunnerId,
            PrincipalId = entity.PrincipalId,
            PrincipalDiscriminator = entity.PrincipalDiscriminator,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(RunnerRoleAssignment entity, RunnerRoleAssignmentUpdateDto dto)
    {
        entity.RunnerId = dto.RunnerId;
        entity.PrincipalDiscriminator = dto.PrincipalDiscriminator;
        entity.RoleName = dto.RoleName;

        // Update type-specific properties based on discriminator
        switch (entity)
        {
            case UserRunnerRoleAssignment userEntity:
                userEntity.UserId = dto.PrincipalId;
                break;
            case ServicePrincipalRunnerRoleAssignment spEntity:
                spEntity.ServicePrincipalId = dto.PrincipalId;
                break;
            case GroupRunnerRoleAssignment groupEntity:
                groupEntity.GroupId = dto.PrincipalId;
                break;
        }
    }
}