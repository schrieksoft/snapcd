using SnapCd.Contracts.Dto.RunnerStackAssignments;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class RunnerStackAssignmentService : GenericCrudService<
    RunnerStackAssignment,
    RunnerStackAssignmentCreateDto,
    RunnerStackAssignmentUpdateDto,
    RunnerStackAssignmentReadDto,
    RunnerStackAssignmentSecuredRepository,
    RunnerStackAssignmentRepository,
    RunnerStackAssignmentCreatedEvent,
    RunnerStackAssignmentUpdatedEvent,
    RunnerStackAssignmentDeletedEvent,
    RunnerStackAssignmentRepositorySettings>
{
    public RunnerStackAssignmentService(
        RunnerStackAssignmentSecuredRepository securedRepository) : base(securedRepository)
    {
    }


    protected override RunnerStackAssignment MapToEntity(RunnerStackAssignmentCreateDto dto, Guid organizationId)
    {
        return RunnerStackAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerStackAssignmentReadDto MapToDto(RunnerStackAssignment entity)
    {
        return RunnerStackAssignmentMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(RunnerStackAssignment entity, RunnerStackAssignmentUpdateDto dto)
    {
        RunnerStackAssignmentMapper.UpdateEntity(entity, dto);
    }
}