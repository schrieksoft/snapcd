using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleHooks;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.ModuleHook)]
public class ModuleHookController : GenericCrudController<
    ModuleHook,
    ModuleHookCreateDto,
    ModuleHookUpdateDto,
    ModuleHookReadDto,
    ModuleHookSecuredRepository,
    ModuleHookRepository,
    ModuleHookService,
    ModuleHookCreatedEvent,
    ModuleHookUpdatedEvent,
    ModuleHookDeletedEvent,
    ModuleHookRepositorySettings>
{
    public ModuleHookController(ModuleHookService service) : base(service)
    {
    }
}
