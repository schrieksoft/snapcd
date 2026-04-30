using SnapCd.Contracts.Dto.RunnerNamespaceAssignments;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class RunnerNamespaceAssignmentService : GenericCrudService<
    RunnerNamespaceAssignment,
    RunnerNamespaceAssignmentCreateDto,
    RunnerNamespaceAssignmentUpdateDto,
    RunnerNamespaceAssignmentReadDto,
    RunnerNamespaceAssignmentSecuredRepository,
    RunnerNamespaceAssignmentRepository,
    RunnerNamespaceAssignmentCreatedEvent,
    RunnerNamespaceAssignmentUpdatedEvent,
    RunnerNamespaceAssignmentDeletedEvent,
    RunnerNamespaceAssignmentRepositorySettings>
{
    public RunnerNamespaceAssignmentService(
        RunnerNamespaceAssignmentSecuredRepository securedRepository) : base(securedRepository)
    {
    }


    protected override RunnerNamespaceAssignment MapToEntity(RunnerNamespaceAssignmentCreateDto dto, Guid organizationId)
    {
        return RunnerNamespaceAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerNamespaceAssignmentReadDto MapToDto(RunnerNamespaceAssignment entity)
    {
        return RunnerNamespaceAssignmentMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(RunnerNamespaceAssignment entity, RunnerNamespaceAssignmentUpdateDto dto)
    {
        RunnerNamespaceAssignmentMapper.UpdateEntity(entity, dto);
    }
}