using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleTerraformFlags;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.ModuleTerraformFlag)]
public class ModuleTerraformFlagController : GenericCrudController<
    ModuleTerraformFlag,
    ModuleTerraformFlagCreateDto,
    ModuleTerraformFlagUpdateDto,
    ModuleTerraformFlagReadDto,
    ModuleTerraformFlagSecuredRepository,
    ModuleTerraformFlagRepository,
    ModuleTerraformFlagService,
    ModuleTerraformFlagCreatedEvent,
    ModuleTerraformFlagUpdatedEvent,
    ModuleTerraformFlagDeletedEvent,
    ModuleTerraformFlagRepositorySettings>
{
    public ModuleTerraformFlagController(ModuleTerraformFlagService service) : base(service)
    {
    }
}
