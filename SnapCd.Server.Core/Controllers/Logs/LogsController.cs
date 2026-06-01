// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Controllers.Logs;

[ApiController]
[Authorize("BearerPolicy")]
[Route(ControllerEndpoints.Logs)]
[OrganizationScopedFeature]
public class LogsController : ControllerBase
{
    private readonly LogService _logService;
    private readonly IBus _bus;
    private readonly SnapCdDbContext _dbContext;
    private readonly ModuleJobSecuredRepository _moduleJobSecuredRepository;


    public LogsController(
        LogService logService,
        IBus bus,
        SnapCdDbContext dbContext,
        ModuleJobSecuredRepository moduleJobSecuredRepository
    )
    {
        _logService = logService;
        _bus = bus;
        _dbContext = dbContext;
        _moduleJobSecuredRepository = moduleJobSecuredRepository;
    }

    [HttpPost]
    public async Task<IActionResult> PostLogs(Guid organizationId, [FromBody] List<LogEntryDto> logEntries)
    {
        // Handle empty log entries
        if (logEntries.Count == 0) return Ok();

        var obj = logEntries
            .Select(x => new { CorrelationId = x.JobId, x.ModuleId })
            .Distinct()
            .First();

        try
        {
            await _logService.AddLogEntries(logEntries);

            await _bus.Publish(new LogReceivedEvent
            {
                JobId = obj.CorrelationId,
                ModuleId = obj.ModuleId
            }, context => { context.TimeToLive = TimeSpan.FromSeconds(60); });
        }
        catch (PrincipalNotAuthorizedException e)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var logEntry = logEntries
                    .Select(x => new LogEntryDto
                    {
                        JobId = x.JobId,
                        ModuleId = x.ModuleId,
                        StackId = x.StackId,
                        NamespaceId = x.NamespaceId,
                        StackName = x.StackName,
                        NamespaceName = x.NamespaceName,
                        TaskName = x.TaskName,
                        Level = LogEventLevel.Error,
                        Message = e.Message,
                        Timestamp = now,
                        BatchTimeStamp = now
                    })
                    .Distinct()
                    .First();


                await _logService.AddLogEntries(new List<LogEntryDto> { logEntry });

                await _bus.Publish(new LogReceivedEvent
                {
                    JobId = obj.CorrelationId,
                    ModuleId = obj.ModuleId
                }, context => { context.TimeToLive = TimeSpan.FromSeconds(60); });

                return StatusCode(StatusCodes.Status403Forbidden, e.Message);
            }
            catch (Exception innerException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"{innerException.Message}.\nThe above error occurred while trying to log the following error: {e.Message}");
            }
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }


        return Ok();
    }

    [HttpGet("{correlationId}")]
    public async Task<List<LogEntryDto>> GetLogs(Guid organizationId, Guid correlationId)
    {
        // ServicePrincipals with Runner assignments can only CREATE logs, not read them
        // Only Users with proper role-based permissions can read logs
        if (_moduleJobSecuredRepository.PrincipalDiscriminator == PrincipalDiscriminator.ServicePrincipal)
            throw new PrincipalNotAuthorizedException(
                "Service Principals cannot read logs. Only Users with proper permissions can read logs."
            );

        // Check if user has Read permission on the ModuleJob entity
        if (!_moduleJobSecuredRepository.CanRead(correlationId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal does not have permission to read logs for ModuleJob '{correlationId}'"
            );

        return await _logService.GetLogEntries(correlationId);
    }

    [HttpGet("string/{correlationId}")]
    public async Task<string> GetLogString(Guid organizationId, Guid correlationId)
    {
        // ServicePrincipals with Runner assignments can only CREATE logs, not read them
        // Only Users with proper role-based permissions can read logs
        if (_moduleJobSecuredRepository.PrincipalDiscriminator == PrincipalDiscriminator.ServicePrincipal)
            throw new PrincipalNotAuthorizedException(
                "Service Principals cannot read logs. Only Users with proper permissions can read logs."
            );

        // Check if user has Read permission on the ModuleJob entity
        if (!_moduleJobSecuredRepository.CanRead(correlationId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal does not have permission to read logs for ModuleJob '{correlationId}'"
            );

        return await _logService.GetLogString(correlationId);
    }

}