using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Controllers.Crud.Generic;

[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
public abstract class GenericCrudController<TEntity, TCreateDto, TUpdateDto, TDto, TSecuredRepository, TRepository, TService, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : ControllerBase
    where TEntity : class, IEntity
    where TCreateDto : class
    where TUpdateDto : class
    where TDto : class
    where TRepository : GenericRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TSecuredRepository : GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TService : GenericCrudService<TEntity, TCreateDto, TUpdateDto, TDto, TSecuredRepository, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings

{
    protected readonly TService Service;

    protected GenericCrudController(TService service)
    {
        Service = service;
    }


    [HttpPost]
    public virtual async Task<ActionResult<TDto>> Create(Guid organizationId, TCreateDto dto)
    {
        try
        {
            var createdEntity = await Service.Create(dto, organizationId);
            return Ok(createdEntity);
        }
        catch (UniqueConstraintException e)
        {
            return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message);
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

    [HttpGet]
    public virtual async Task<ActionResult<List<TDto>>> List(Guid organizationId)
    {
        try
        {
            var entities = await Service.List(organizationId);
            return Ok(entities);
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

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<TDto>> Get(Guid organizationId, Guid id)
    {
        try
        {
            var entity = await Service.Get(id, organizationId);
            return Ok(entity);
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

    [HttpPut("{id}")]
    public virtual async Task<ActionResult<TDto>> Update(Guid organizationId, TUpdateDto dto, Guid id)
    {
        try
        {
            var updatedEntity = await Service.Update(dto, id, organizationId);
            return Ok(updatedEntity);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (UniqueConstraintException e)
        {
            return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message);
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

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        try
        {
            await Service.Delete(id, organizationId);
            return NoContent();
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