using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleTerraformArrayFlags;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.ModuleTerraformArrayFlag)]
public class ModuleTerraformArrayFlagController : GenericCrudController<
    ModuleTerraformArrayFlag,
    ModuleTerraformArrayFlagCreateDto,
    ModuleTerraformArrayFlagUpdateDto,
    ModuleTerraformArrayFlagReadDto,
    ModuleTerraformArrayFlagSecuredRepository,
    ModuleTerraformArrayFlagRepository,
    ModuleTerraformArrayFlagService,
    ModuleTerraformArrayFlagCreatedEvent,
    ModuleTerraformArrayFlagUpdatedEvent,
    ModuleTerraformArrayFlagDeletedEvent,
    ModuleTerraformArrayFlagRepositorySettings>
{
    public ModuleTerraformArrayFlagController(ModuleTerraformArrayFlagService service) : base(service)
    {
    }
}
