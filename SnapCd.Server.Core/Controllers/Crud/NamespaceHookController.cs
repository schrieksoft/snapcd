using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.NamespaceHooks;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.NamespaceHook)]
public class NamespaceHookController : GenericCrudController<
    NamespaceHook,
    NamespaceHookCreateDto,
    NamespaceHookUpdateDto,
    NamespaceHookReadDto,
    NamespaceHookSecuredRepository,
    NamespaceHookRepository,
    NamespaceHookService,
    NamespaceHookCreatedEvent,
    NamespaceHookUpdatedEvent,
    NamespaceHookDeletedEvent,
    NamespaceHookRepositorySettings>
{
    public NamespaceHookController(NamespaceHookService service) : base(service)
    {
    }
}
