using SnapCd.Contracts.Dto.RunnerModuleAssignments;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class RunnerModuleAssignmentService : GenericCrudService<
    RunnerModuleAssignment,
    RunnerModuleAssignmentCreateDto,
    RunnerModuleAssignmentUpdateDto,
    RunnerModuleAssignmentReadDto,
    RunnerModuleAssignmentSecuredRepository,
    RunnerModuleAssignmentRepository,
    RunnerModuleAssignmentCreatedEvent,
    RunnerModuleAssignmentUpdatedEvent,
    RunnerModuleAssignmentDeletedEvent,
    RunnerModuleAssignmentRepositorySettings>
{
    public RunnerModuleAssignmentService(
        RunnerModuleAssignmentSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override RunnerModuleAssignment MapToEntity(RunnerModuleAssignmentCreateDto dto, Guid organizationId)
    {
        return RunnerModuleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerModuleAssignmentReadDto MapToDto(RunnerModuleAssignment entity)
    {
        return RunnerModuleAssignmentMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(RunnerModuleAssignment entity, RunnerModuleAssignmentUpdateDto dto)
    {
        RunnerModuleAssignmentMapper.UpdateEntity(entity, dto);
    }
}