using SnapCd.Contracts.Dto.RunnerNamespaceAssignments;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;

namespace SnapCd.Server.Core.Mappers;

public static class RunnerNamespaceAssignmentMapper
{
    public static RunnerNamespaceAssignment ToEntity(RunnerNamespaceAssignmentCreateDto dto, Guid organizationId)
    {
        return new RunnerNamespaceAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            RunnerId = dto.RunnerId
        };
    }

    public static RunnerNamespaceAssignmentReadDto ToDto(RunnerNamespaceAssignment entity)
    {
        return new RunnerNamespaceAssignmentReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            RunnerId = entity.RunnerId
        };
    }

    public static void UpdateEntity(RunnerNamespaceAssignment entity, RunnerNamespaceAssignmentUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.RunnerId = dto.RunnerId;
    }
}