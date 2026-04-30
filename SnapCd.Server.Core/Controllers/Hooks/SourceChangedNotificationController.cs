using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Controllers.Hooks;

[Route(ControllerEndpoints.SourceChangedNotification)]
[ApiController]
[Authorize("BearerPolicy")]
public class SourceChangedNotificationController : ControllerBase
{
    private readonly SourceChangedService _sourceChangedService;

    public SourceChangedNotificationController(SourceChangedService sourceChangedService)
    {
        _sourceChangedService = sourceChangedService;
    }

    [HttpPost]
    public async Task<IActionResult> NotifyChange(Guid organizationId, SourceChangedDto dto)
    {
        try
        {
            await _sourceChangedService.NotifyChange(dto, organizationId);
            return Ok($"Source change has been notified");
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}