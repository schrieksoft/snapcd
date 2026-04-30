using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.NamespacePulumiFlags;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.NamespacePulumiFlag)]
public class NamespacePulumiFlagController : GenericCrudController<
    NamespacePulumiFlag,
    NamespacePulumiFlagCreateDto,
    NamespacePulumiFlagUpdateDto,
    NamespacePulumiFlagReadDto,
    NamespacePulumiFlagSecuredRepository,
    NamespacePulumiFlagRepository,
    NamespacePulumiFlagService,
    NamespacePulumiFlagCreatedEvent,
    NamespacePulumiFlagUpdatedEvent,
    NamespacePulumiFlagDeletedEvent,
    NamespacePulumiFlagRepositorySettings>
{
    public NamespacePulumiFlagController(NamespacePulumiFlagService service) : base(service)
    {
    }
}
