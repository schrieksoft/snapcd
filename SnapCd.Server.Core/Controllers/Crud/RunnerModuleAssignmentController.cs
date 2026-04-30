using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.RunnerModuleAssignments;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class RunnerModuleAssignmentCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.RunnerModuleAssignment)]
public class RunnerModuleAssignmentController : GenericCrudController<
    RunnerModuleAssignment,
    RunnerModuleAssignmentCreateDto,
    RunnerModuleAssignmentUpdateDto,
    RunnerModuleAssignmentReadDto,
    RunnerModuleAssignmentSecuredRepository,
    RunnerModuleAssignmentRepository,
    RunnerModuleAssignmentService,
    RunnerModuleAssignmentCreatedEvent,
    RunnerModuleAssignmentUpdatedEvent,
    RunnerModuleAssignmentDeletedEvent,
    RunnerModuleAssignmentRepositorySettings>
{
    public RunnerModuleAssignmentController(RunnerModuleAssignmentService service) : base(service)
    {
    }
}