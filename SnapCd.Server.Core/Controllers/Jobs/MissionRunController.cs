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
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Ai.Missions;

namespace SnapCd.Server.Core.Controllers.Jobs;

[Route(ControllerEndpoints.MissionRun)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
[PermissionSource(Repository = typeof(ModuleJobSecuredRepository), Verb = PermissionVerb.RunJob)]
public class MissionRunController : ControllerBase
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IBus _bus;
    private readonly MissionDispatcher _dispatcher;
    private readonly ModuleJobSecuredRepositoryFactory _moduleJobSecuredRepositoryFactory;
    private readonly ILogger<MissionRunController> _logger;

    public MissionRunController(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IBus bus,
        MissionDispatcher dispatcher,
        ModuleJobSecuredRepositoryFactory moduleJobSecuredRepositoryFactory,
        ILogger<MissionRunController> logger)
    {
        _dbContextFactory = dbContextFactory;
        _bus = bus;
        _dispatcher = dispatcher;
        _moduleJobSecuredRepositoryFactory = moduleJobSecuredRepositoryFactory;
        _logger = logger;
    }

    private bool CanRunJobForModuleJob(SnapCdDbContext db, Guid moduleJobId, Guid organizationId)
    {
        var moduleId = db.ModuleJobs
            .Where(j => j.Id == moduleJobId && j.OrganizationId == organizationId)
            .Select(j => j.ModuleId)
            .FirstOrDefault();
        if (moduleId == Guid.Empty)
            return false;

        using var repo = _moduleJobSecuredRepositoryFactory.Create();
        return repo.CanRunJob(moduleId, organizationId);
    }

    [EndpointSummary("Cancel an in-flight mission run")]
    [HttpPost("{runId}/cancel")]
    public async Task<IActionResult> Cancel(Guid organizationId, Guid runId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var run = await db.ModuleJobMissionRuns
            .FirstOrDefaultAsync(r => r.Id == runId && r.OrganizationId == organizationId);
        if (run is null)
            return NotFound($"Run '{runId}' not found");

        if (!CanRunJobForModuleJob(db, run.ModuleJobId, organizationId))
            return StatusCode(StatusCodes.Status403Forbidden,
                "Not permitted to manage runs for this module's jobs.");

        if (run.Status is MissionStatus.Succeeded or MissionStatus.Failed
            or MissionStatus.Cancelled or MissionStatus.TimedOut)
            return Conflict($"Run '{runId}' is not active (status {run.Status}).");

        if (run.ServerInstanceId is null || run.SignalRConnectionId is null)
            return Conflict($"Run '{runId}' has no owning connection to cancel.");

        run.CancelRequestedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var uri = MassTransitHelpers.GetAgentConsumerEndpoint(run.ServerInstanceId.Value, nameof(CancelMissionRunRequested));
        var endpoint = await _bus.GetSendEndpoint(new Uri(uri));
        await endpoint.Send(new CancelMissionRunRequested
        {
            RunId = run.Id,
            OrganizationId = organizationId,
            InvocationId = run.InvocationId,
            AgentConnectionId = run.SignalRConnectionId
        });

        return Ok($"Cancellation requested for run '{runId}'.");
    }

    [EndpointSummary("Rerun a mission")]
    [HttpPost("rerun/{missionId}")]
    public async Task<IActionResult> Rerun(Guid organizationId, Guid missionId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var mission = await db.ModuleJobMissions
            .FirstOrDefaultAsync(m => m.Id == missionId && m.OrganizationId == organizationId);
        if (mission is null)
            return NotFound($"Mission '{missionId}' not found");

        if (!CanRunJobForModuleJob(db, mission.ModuleJobId, organizationId))
            return StatusCode(StatusCodes.Status403Forbidden,
                "Not permitted to manage runs for this module's jobs.");

        var maxAttempt = await db.ModuleJobMissionRuns
            .Where(r => r.ModuleJobMissionId == missionId && r.OrganizationId == organizationId)
            .Select(r => (int?)r.AttemptNumber)
            .MaxAsync() ?? 0;

        var outcome = await _dispatcher.DispatchAsync(db, mission, mission.AgentId, maxAttempt + 1, HttpContext.RequestAborted);
        return outcome switch
        {
            MissionDispatchOutcome.Dispatched => Ok($"Rerun started for mission '{missionId}' (attempt {maxAttempt + 1})."),
            MissionDispatchOutcome.AlreadyActive => Conflict($"Mission '{missionId}' already has an active run."),
            MissionDispatchOutcome.NoLiveConnection => Conflict($"Agent for mission '{missionId}' has no live connection."),
            MissionDispatchOutcome.NotLicensed => StatusCode(StatusCodes.Status403Forbidden, "AI Agents are not licensed for this organization."),
            MissionDispatchOutcome.AgentNotAssigned => UnprocessableEntity($"Agent for mission '{missionId}' is not assigned to the target scope. The Agent owner must assign the Agent (or set IsSuppliedToAllModules) before this mission can be re-run."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
