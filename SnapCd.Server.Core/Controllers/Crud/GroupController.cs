using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Groups;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Licensing.Attributes;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class GroupCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.Group)]
public class GroupController : GenericCrudController<
    Group,
    GroupCreateDto,
    GroupUpdateDto,
    GroupReadDto,
    GroupSecuredRepository,
    GroupRepository,
    GroupService,
    GroupCreatedEvent,
    GroupUpdatedEvent,
    GroupDeletedEvent,
    GroupRepositorySettings>
{
    public GroupController(GroupService service) : base(service)
    {
    }

    [VerifyLicense("GroupRbac")]
    public override Task<ActionResult<GroupReadDto>> Create(Guid organizationId, GroupCreateDto dto)
    {
        return base.Create(organizationId, dto);
    }

    [VerifyLicense("GroupRbac")]
    public override Task<ActionResult<GroupReadDto>> Update(Guid organizationId, GroupUpdateDto dto, Guid id)
    {
        return base.Update(organizationId, dto, id);
    }

    [HttpGet($"{GroupCustomEndpointNames.GetByName}/{{name}}")]
    public async Task<ActionResult<GroupReadDto>> GetByName(Guid organizationId, string name)
    {
        try
        {
            var groupDto = await Service.GetByName(name, organizationId);
            return Ok(groupDto);
        }

        catch (EntityNotFoundException e)
        {
            return StatusCode(CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}