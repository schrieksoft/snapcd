using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.RunnerNamespaceAssignments;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class RunnerNamespaceAssignmentCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.RunnerNamespaceAssignment)]
public class RunnerNamespaceAssignmentController : GenericCrudController<
    RunnerNamespaceAssignment,
    RunnerNamespaceAssignmentCreateDto,
    RunnerNamespaceAssignmentUpdateDto,
    RunnerNamespaceAssignmentReadDto,
    RunnerNamespaceAssignmentSecuredRepository,
    RunnerNamespaceAssignmentRepository,
    RunnerNamespaceAssignmentService,
    RunnerNamespaceAssignmentCreatedEvent,
    RunnerNamespaceAssignmentUpdatedEvent,
    RunnerNamespaceAssignmentDeletedEvent,
    RunnerNamespaceAssignmentRepositorySettings>
{
    public RunnerNamespaceAssignmentController(RunnerNamespaceAssignmentService service) : base(service)
    {
    }
}