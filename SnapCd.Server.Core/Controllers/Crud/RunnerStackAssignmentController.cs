using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.RunnerStackAssignments;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class RunnerStackAssignmentCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.RunnerStackAssignment)]
public class RunnerStackAssignmentController : GenericCrudController<
    RunnerStackAssignment,
    RunnerStackAssignmentCreateDto,
    RunnerStackAssignmentUpdateDto,
    RunnerStackAssignmentReadDto,
    RunnerStackAssignmentSecuredRepository,
    RunnerStackAssignmentRepository,
    RunnerStackAssignmentService,
    RunnerStackAssignmentCreatedEvent,
    RunnerStackAssignmentUpdatedEvent,
    RunnerStackAssignmentDeletedEvent,
    RunnerStackAssignmentRepositorySettings>
{
    public RunnerStackAssignmentController(RunnerStackAssignmentService service) : base(service)
    {
    }
}