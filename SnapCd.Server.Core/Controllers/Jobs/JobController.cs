using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud.Jobs;

namespace SnapCd.Server.Core.Controllers.Jobs;

[Route(ControllerEndpoints.Jobs)]
[ApiController]
[Authorize("BearerPolicy")]
public class JobController : ControllerBase
{
    private readonly SecuredJobServiceFactory _securedJobServiceFactory;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public JobController(SecuredJobServiceFactory securedJobServiceFactory, IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _securedJobServiceFactory = securedJobServiceFactory;
        _dbContextFactory = dbContextFactory;
    }

    [HttpPost("apply/{id}")]
    public async Task<IActionResult> Apply(Guid organizationId, Guid id, [FromQuery] Guid? correlationId)
    {
        try
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var module = await dbContext.Modules
                .Where(m => m.Id == id && m.OrganizationId == organizationId)
                .Select(m => new { m.NamespaceId })
                .FirstOrDefaultAsync();

            if (module == null) return NotFound($"Module '{id}' not found");

            using var gatekeepingJobService = _securedJobServiceFactory.Create();
            var jobId = correlationId ?? Guid.NewGuid();
            await gatekeepingJobService.Apply(id, organizationId, jobId);
            return Ok($"Module '{id}' apply started");
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpPost("destroy/{id}")]
    public async Task<IActionResult> Destroy(Guid organizationId, Guid id, [FromQuery] Guid? correlationId)
    {
        try
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var module = await dbContext.Modules
                .Where(m => m.Id == id && m.OrganizationId == organizationId)
                .Select(m => new { m.NamespaceId })
                .FirstOrDefaultAsync();

            if (module == null) return NotFound($"Module '{id}' not found");

            using var gatekeepingJobService = _securedJobServiceFactory.Create();
            var jobId = correlationId ?? Guid.NewGuid();
            await gatekeepingJobService.Destroy(id, organizationId, jobId);
            return Ok($"Module '{id}' destroy started");
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}