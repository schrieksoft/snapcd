using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.NamespacePulumiArrayFlag)]
public class NamespacePulumiArrayFlagController : GenericCrudController<
    NamespacePulumiArrayFlag,
    NamespacePulumiArrayFlagCreateDto,
    NamespacePulumiArrayFlagUpdateDto,
    NamespacePulumiArrayFlagReadDto,
    NamespacePulumiArrayFlagSecuredRepository,
    NamespacePulumiArrayFlagRepository,
    NamespacePulumiArrayFlagService,
    NamespacePulumiArrayFlagCreatedEvent,
    NamespacePulumiArrayFlagUpdatedEvent,
    NamespacePulumiArrayFlagDeletedEvent,
    NamespacePulumiArrayFlagRepositorySettings>
{
    public NamespacePulumiArrayFlagController(NamespacePulumiArrayFlagService service) : base(service)
    {
    }
}
