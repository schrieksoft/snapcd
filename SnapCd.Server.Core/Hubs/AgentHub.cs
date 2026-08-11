// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.AgentResults;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Agents;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Hubs;

/// <summary>
/// SignalR hub the SnapCd.Agent orchestrator connects to. Server pushes domain events
/// (ModuleJob.*, Module.*, Stack.*, Secret.Rotated, Mission.*) to subscribed agents.
/// Outbound calls go via REST + MCP, not via hub methods — see ai-agent.md.
/// </summary>
[Authorize(AuthenticationSchemes = "Bearer")]
public class AgentHub : Hub
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IBus _bus;
    private readonly ILogger<AgentHub> _logger;
    private readonly ServerSettings _serverSettings;
    private readonly AgentConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly Services.AgentConnectionValidator.AgentConnectionValidator _connectionValidator;
    private readonly ILicenseInfoProvider _licenseInfoProvider;
    private readonly ModuleJobMissionRunMilestoneRepositoryFactory _milestoneRepositoryFactory;
    private readonly ModuleJobMissionRunRepositoryFactory _runRepositoryFactory;

    public AgentHub(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IBus bus,
        ILogger<AgentHub> logger,
        IOptions<ServerSettings> serverSettings,
        AgentConnectionRepositoryFactory connectionRepositoryFactory,
        Services.AgentConnectionValidator.AgentConnectionValidator connectionValidator,
        ILicenseInfoProvider licenseInfoProvider,
        ModuleJobMissionRunMilestoneRepositoryFactory milestoneRepositoryFactory,
        ModuleJobMissionRunRepositoryFactory runRepositoryFactory)
    {
        _dbContextFactory = dbContextFactory;
        _bus = bus;
        _logger = logger;
        _serverSettings = serverSettings.Value;
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _connectionValidator = connectionValidator;
        _licenseInfoProvider = licenseInfoProvider;
        _milestoneRepositoryFactory = milestoneRepositoryFactory;
        _runRepositoryFactory = runRepositoryFactory;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            _logger.LogDebug("New agent connection attempt from {ConnectionId}", Context.ConnectionId);

            var httpContext = Context.GetHttpContext();
            var organizationIdParam = httpContext?.Request.Query["organization_id"].ToString();
            var agentIdParam = httpContext?.Request.Query["agent_id"].ToString();
            var agentInstanceParam = httpContext?.Request.Query["agent_instance"].ToString();

            if (string.IsNullOrEmpty(organizationIdParam) || string.IsNullOrEmpty(agentIdParam))
            {
                _logger.LogWarning(
                    "Missing required query parameters. Provided: organization_id={OrganizationId}, agent_id={AgentId}",
                    organizationIdParam ?? "(null)", agentIdParam ?? "(null)");
                Context.Abort();
                return;
            }

            if (!Guid.TryParse(organizationIdParam, out var organizationId))
            {
                _logger.LogWarning("Invalid organization_id format: {OrganizationId}", organizationIdParam);
                Context.Abort();
                return;
            }

            if (!Guid.TryParse(agentIdParam, out var agentId))
            {
                _logger.LogWarning("Invalid agent_id format: {AgentId}", agentIdParam);
                Context.Abort();
                return;
            }

            var licenseInfo = await _licenseInfoProvider.GetLicenseInfoAsync(organizationId);
            if (!licenseInfo.Includes(Feature.AiAgents))
            {
                _logger.LogWarning(
                    "Organization {OrganizationId} does not have AiAgents licensed; refusing agent connection",
                    organizationId);
                throw new HubException("AI Agents are not licensed for this organization. Upgrade to Enterprise to use this feature.");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var agent = await dbContext.Agents
                .FirstOrDefaultAsync(a => a.Id == agentId && a.OrganizationId == organizationId);

            if (agent == null)
            {
                _logger.LogWarning("Agent {AgentId} not found for organization {OrganizationId}",
                    agentId, organizationId);
                Context.Abort();
                return;
            }

            if (agent.IsDisabled)
            {
                _logger.LogWarning("Agent {AgentId} is disabled; refusing connection", agentId);
                Context.Abort();
                return;
            }

            // Validate instance name based on AllowMultipleInstances flag (mirrors RunnerHub).
            if (agent.AllowMultipleInstances)
            {
                if (string.IsNullOrEmpty(agentInstanceParam))
                {
                    _logger.LogWarning(
                        "Agent {AgentId} requires instance name (AllowMultipleInstances=true) but none provided",
                        agentId);
                    throw new HubException("Agent requires an instance name because AllowMultipleInstances is enabled");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(agentInstanceParam))
                {
                    agentInstanceParam = agent.Name;
                }
            }

            // JWT-claim validation, same as RunnerHub.
            var principalIdClaim = Context.User?.FindFirst(ClaimTypeConstants.SubjectClaimType)?.Value;
            var principalDiscriminatorClaim = Context.User?.FindFirst(ClaimTypeConstants.PrincipalDiscriminatorClaimType)?.Value;

            if (string.IsNullOrEmpty(principalIdClaim) || string.IsNullOrEmpty(principalDiscriminatorClaim))
            {
                _logger.LogWarning("Missing principal claims in JWT token");
                Context.Abort();
                return;
            }

            if (!Guid.TryParse(principalIdClaim, out var principalId))
            {
                _logger.LogWarning("Invalid principal ID in JWT token");
                Context.Abort();
                return;
            }

            if (principalDiscriminatorClaim != "ServicePrincipal")
            {
                _logger.LogWarning(
                    "Only ServicePrincipals can connect as agents. Attempted connection with discriminator: {Discriminator}",
                    principalDiscriminatorClaim);
                Context.Abort();
                return;
            }

            var servicePrincipal = await dbContext.ServicePrincipals
                .FirstOrDefaultAsync(sp => sp.Id == principalId && sp.OrganizationId == organizationId);

            if (servicePrincipal == null)
            {
                _logger.LogWarning(
                    "ServicePrincipal {PrincipalId} not found or not in organization {OrganizationId}",
                    principalId, organizationId);
                Context.Abort();
                return;
            }

            if (agent.ServicePrincipalId != principalId)
            {
                _logger.LogWarning(
                    "ServicePrincipal {PrincipalId} does not match Agent's assigned ServicePrincipal {AgentServicePrincipalId}",
                    principalId, agent.ServicePrincipalId);
                Context.Abort();
                return;
            }

            // agent_id claim validation. The token must have been issued with the agent_id
            // parameter on /connect/token AND the claim must match the agent_id query string.
            // This is what makes "SP token" vs "SP acting as Agent X token" load-bearing:
            // plain SP tokens cannot connect to /agenthub even if they're bound to an Agent.
            var agentIdClaim = Context.User?.FindFirst("agent_id")?.Value;
            if (string.IsNullOrEmpty(agentIdClaim))
            {
                _logger.LogWarning(
                    "agent_id claim missing from JWT — Agent connections require an agent-attributed token");
                Context.Abort();
                return;
            }
            if (!Guid.TryParse(agentIdClaim, out var claimAgentId) || claimAgentId != agentId)
            {
                _logger.LogWarning(
                    "agent_id claim {ClaimAgentId} does not match query-string agent_id {QueryAgentId}",
                    agentIdClaim, agentId);
                Context.Abort();
                return;
            }

            using var connectionRepository = _connectionRepositoryFactory.Create();

            // Single-instance enforcement: if AllowMultipleInstances=false, no other live
            // instance may already be holding the slot.
            if (!agent.AllowMultipleInstances)
            {
                var existing = await connectionRepository.GetActiveConnectionsByAgentId(agentId, organizationId);
                if (existing.Any(x => x.InstanceName != agentInstanceParam))
                {
                    _logger.LogWarning(
                        "Agent {AgentId} does not allow multiple instances. Instance '{ExistingInstance}' is already connected.",
                        agentId, agentInstanceParam);
                    throw new HubException(
                        $"Agent does not allow multiple instances. An instance is already connected as '{agentInstanceParam}'");
                }
            }

            var validationResult = await _connectionValidator.ValidateConnection(organizationId, agentId, agentInstanceParam!);
            if (!validationResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Connection validation failed for agent {InstanceName} (ID: {AgentId}): {Reason}",
                    agentInstanceParam, agentId, validationResult.RejectionReason);
                throw new HubException(validationResult.RejectionReason ?? "Connection validation failed");
            }

            var connectionRecord = new AgentConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AgentId = agentId,
                InstanceName = agentInstanceParam!,
                SignalRConnectionId = Context.ConnectionId,
                ServerInstanceId = _serverSettings.InstanceId
            };
            await connectionRepository.Create(connectionRecord);

            await _bus.Publish(new AgentAvailabilityChangedEvent
            {
                AgentId = agentId,
                AgentInstanceName = agentInstanceParam!
            });

            await _bus.Publish(new AgentReconnectedEvent
            {
                OrganizationId = organizationId,
                AgentId = agentId,
                InstanceName = agentInstanceParam!,
                ServerInstanceId = _serverSettings.InstanceId
            });

            _logger.LogInformation(
                "Agent '{AgentId}' connected with instance name {AgentInstanceName} in organization {OrganizationId}",
                agentId, agentInstanceParam, organizationId);

            await base.OnConnectedAsync();
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in agent connection for {ConnectionId}", Context.ConnectionId);
            Context.Abort();
        }
    }

    // ---- Inbound run callbacks (orchestrator → server), keyed by InvocationId on the run. ----
    // Each resolves the ModuleJobMissionRun, verifies the calling agent owns it, updates it, and
    // projects the outcome onto the parent ModuleJobMission. Logs are dedicated to the run — they
    // deliberately do NOT touch LogService / ModuleJob.Logs.

    /// <summary>No-heartbeat-for-this-long → the run is recovered (≈ 4× the orchestrator's heartbeat interval).</summary>
    private static readonly TimeSpan ServerTimeout = TimeSpan.FromMinutes(2);

    /// <summary>Grace after a disconnect for the orchestrator to reconnect and resume the run.</summary>
    private static readonly TimeSpan ReconnectGrace = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Acknowledge a liveness ping. Invoking this proves the agent can still service hub calls,
    /// which an open connection alone does not.
    /// </summary>
    public Task Pong(Guid pingId)
    {
        Services.AgentLivenessProbe.Acknowledge(pingId);
        return Task.CompletedTask;
    }

    public async Task MissionStarted(Guid invocationId) =>
        await UpdateRunAsync(invocationId, run =>
        {
            run.Status = MissionStatus.Running;
            run.StartedAt = DateTime.UtcNow;
            run.DeadlineAt = DateTime.UtcNow.Add(ServerTimeout);
        });

    /// <summary>
    /// Apply a mutation to the calling agent's run through the non-secured repository, so the run's audit
    /// fields are stamped — attributed to the connecting agent via a <see cref="ClaimsPrincipalProvider"/>
    /// over the hub's <c>Context.User</c> (IHttpContextAccessor is unreliable in a hub) — and, with the
    /// parent-mission projection, committed in the repository's single transaction. The generic
    /// run-updated event is suppressed (EmitUpdateEvents=false on the run repo), so this single-context
    /// flow is correct and the run stays driven by the purpose-built <see cref="MissionRunModifiedEvent"/>.
    /// Returns false if the run wasn't found / isn't owned by the caller.
    /// </summary>
    private async Task<bool> UpdateRunAsync(Guid invocationId, Action<ModuleJobMissionRun> mutate,
        bool project = true, bool publishModified = true, bool suppressUpdateEvent = false)
    {
        // suppressUpdateEvent (e.g. streamed log appends): stamp audit, skip the per-batch event.
        using var repo = _runRepositoryFactory.Create(
            new ClaimsPrincipalProvider(Context.User), suppressEvents: suppressUpdateEvent);
        // Load detached so the repository re-reads the unmodified row as the event's "previous" state.
        var run = await ResolveRunAsync(repo.DbContext, invocationId, track: false);
        if (run is null) return false;
        mutate(run);
        if (project) await ProjectAsync(repo.DbContext, run);
        await repo.Update(run);
        if (publishModified) await PublishRunModified(run);
        return true;
    }

    /// <summary>Dedicated periodic liveness ping (the twin of RunnerHub's ReportRunningTask) — fires on
    /// the orchestrator's timer regardless of log output. Bumps the watchdog deadline and resumes a run
    /// parked <c>AwaitingReconnect</c>.</summary>
    public async Task MissionHeartbeat(Guid invocationId, DateTime? lastEventAt)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var run = await ResolveRunAsync(db, invocationId);
        if (run is null) return;
        if (run.Status is MissionStatus.Succeeded or MissionStatus.Failed
            or MissionStatus.Cancelled or MissionStatus.TimedOut) return;

        if (run.Status == MissionStatus.AwaitingReconnect)
        {
            run.Status = MissionStatus.Running;
            run.SignalRConnectionId = Context.ConnectionId; // re-bind to the resumed connection
            run.ServerInstanceId = _serverSettings.InstanceId;
            await ProjectAsync(db, run);
        }

        run.DeadlineAt = DateTime.UtcNow.Add(ServerTimeout);
        if (lastEventAt is not null) run.LastEventAt = lastEventAt;
        await db.SaveChangesAsync();
    }

    public async Task AddMissionLogs(Guid invocationId, List<MissionLogLineDto> lines)
    {
        if (lines is null || lines.Count == 0) return;
        await UpdateRunAsync(invocationId, run =>
        {
            var builder = new StringBuilder(run.Logs ?? string.Empty);
            foreach (var line in lines)
                builder.Append($"{line.Timestamp:O} [{line.Level}] {line.Message}\n");
            run.Logs = builder.ToString();
            run.LastEventAt = DateTime.UtcNow; // progress, not liveness — the heartbeat owns the deadline
        }, project: false, suppressUpdateEvent: true); // log append: stamp audit, but no per-batch event
    }

    /// <summary>A curated progress checkpoint reported mid-mission (the agent's <c>report_milestone</c>
    /// tool). Persists it to the run's timeline, refreshes the live UI, and publishes the
    /// <see cref="MissionMilestoneReported"/> domain event the Integrations feature will subscribe to.</summary>
    public async Task AddMissionMilestone(Guid invocationId, MissionMilestoneDto milestone)
    {
        if (milestone is null || string.IsNullOrWhiteSpace(milestone.Message)) return;
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var run = await ResolveRunAsync(db, invocationId);
        if (run is null) return;

        var reportedAt = milestone.Timestamp == default ? DateTime.UtcNow : milestone.Timestamp.UtcDateTime;

        // Persist through the non-secured repository so the audit fields are stamped and the generic
        // CreatedEvent is emitted automatically. The principal comes from the hub's Context.User (the
        // connecting agent) via ClaimsPrincipalProvider — IHttpContextAccessor is unreliable in a hub,
        // so the default HttpContext-based provider would attribute the row to System instead.
        using (var milestones = _milestoneRepositoryFactory.Create(new ClaimsPrincipalProvider(Context.User)))
        {
            await milestones.Create(new ModuleJobMissionRunMilestone
            {
                Id = NewId.NextGuid(),
                OrganizationId = run.OrganizationId,
                ModuleJobMissionRunId = run.Id,
                Kind = milestone.Kind,
                Message = milestone.Message,
                ReportedAt = reportedAt
            });
        }

        // Run-level progress + live UI refresh (a separate concern from the milestone row).
        run.LastEventAt = DateTime.UtcNow; // progress, not liveness — the heartbeat owns the deadline
        await db.SaveChangesAsync();
        await PublishRunModified(run);

        // Domain event the Integrations feature subscribes to (carries ModuleJobMissionId, which the
        // milestone row doesn't denormalise).
        await _bus.Publish(new MissionMilestoneReported
        {
            OrganizationId = run.OrganizationId,
            ModuleJobId = run.ModuleJobId,
            ModuleJobMissionId = run.ModuleJobMissionId,
            ModuleJobMissionRunId = run.Id,
            MissionType = run.MissionType,
            Kind = milestone.Kind,
            Message = milestone.Message,
            ReportedAt = reportedAt
        });
    }

    public async Task MissionCompleted(Guid invocationId, MissionResultDto result)
    {
        var updated = await UpdateRunAsync(invocationId, run =>
        {
            run.Status = result.Success ? MissionStatus.Succeeded : MissionStatus.Failed;
            run.ResultSummary = result.Summary;
            run.Error = JoinErrorAndDetail(result.Error, result.Detail);
            run.ToolCallsJson = result.ToolCallsJson;
            run.TokensJson = result.TokensJson;
            run.DurationSeconds = result.DurationSeconds;
            run.DiagnosisCategory = result.DiagnosisCategory;
            run.CompletedAt = DateTime.UtcNow;
        });
        if (!updated) return;

        if (result.Success)
        {
            _logger.LogInformation("Mission run {InvocationId} succeeded", invocationId);
        }
        else
        {
            _logger.LogWarning(
                "Run {InvocationId} completed: success=False, error={Error}, detail={Detail}, summary={Summary}",
                invocationId, result.Error, result.Detail, result.Summary);
        }
    }

    public async Task MissionFaulted(Guid invocationId, string? error, string? detail) =>
        await UpdateRunAsync(invocationId, run =>
        {
            run.Status = MissionStatus.Failed;
            run.Error = JoinErrorAndDetail(error, detail);
            run.CompletedAt = DateTime.UtcNow;
        });

    /// <summary>Orchestrator confirms a cancel landed (the sidecar invoke was aborted).</summary>
    public async Task MissionCancelled(Guid invocationId) =>
        await UpdateRunAsync(invocationId, run =>
        {
            run.Status = MissionStatus.Cancelled;
            run.CompletedAt = DateTime.UtcNow;
        });

    /// <summary>Publish a run-modified notification so subscribed UI components (e.g. the Missions tab
    /// on a ModuleJob) refresh. Fires on status transitions and on log-batch appends so the live view
    /// updates as the run streams; heartbeats stay quiet because they don't change the projected state.</summary>
    private Task PublishRunModified(ModuleJobMissionRun run) =>
        _bus.Publish(new MissionRunModifiedEvent
        {
            OrganizationId = run.OrganizationId,
            ModuleJobId = run.ModuleJobId,
            RunId = run.Id
        });

    /// <summary>Merge the agent's short error code and longer detail into a single string so the UI's
    /// error pane carries both — e.g. "ToolError: MCP server returned 401". Null if both are empty.</summary>
    private static string? JoinErrorAndDetail(string? error, string? detail)
    {
        var hasError = !string.IsNullOrWhiteSpace(error);
        var hasDetail = !string.IsNullOrWhiteSpace(detail);
        if (hasError && hasDetail) return $"{error}: {detail}";
        if (hasError) return error;
        if (hasDetail) return detail;
        return null;
    }

    /// <summary>Project a run's status/result onto its parent ModuleJobMission (the latest run wins).</summary>
    private static async Task ProjectAsync(SnapCdDbContext db, ModuleJobMissionRun run)
    {
        var mission = await db.ModuleJobMissions
            .FirstOrDefaultAsync(m => m.Id == run.ModuleJobMissionId && m.OrganizationId == run.OrganizationId);
        if (mission is null) return;
        mission.Status = run.Status;
        mission.ResultSummary = run.ResultSummary;
        mission.Error = run.Error;
        mission.CompletedAt = run.CompletedAt;
    }

    /// <summary>Resolve the run for the calling agent: by InvocationId within the connection's org, and
    /// only if it belongs to the agent the JWT is attributed to. Pass <paramref name="track"/> = false to
    /// load it detached (so a subsequent repository update re-reads the unmodified row as the event's
    /// "previous" state).</summary>
    private async Task<ModuleJobMissionRun?> ResolveRunAsync(SnapCdDbContext db, Guid invocationId, bool track = true)
    {
        var httpContext = Context.GetHttpContext();
        if (!Guid.TryParse(httpContext?.Request.Query["organization_id"].ToString(), out var organizationId))
            return null;
        Guid.TryParse(Context.User?.FindFirst("agent_id")?.Value, out var agentId);

        var query = track ? db.ModuleJobMissionRuns : db.ModuleJobMissionRuns.AsNoTracking();
        var run = await query
            .FirstOrDefaultAsync(r => r.InvocationId == invocationId && r.OrganizationId == organizationId);
        if (run is null)
        {
            _logger.LogWarning("No ModuleJobMissionRun for invocation {InvocationId} in org {OrganizationId}",
                invocationId, organizationId);
            return null;
        }
        if (run.AgentId != agentId)
        {
            _logger.LogWarning("Agent {AgentId} reported for run {InvocationId} it does not own", agentId, invocationId);
            return null;
        }
        return run;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            using var connectionRepository = _connectionRepositoryFactory.Create();
            var httpContext = Context.GetHttpContext();
            var organizationIdParam = httpContext?.Request.Query["organization_id"].ToString();
            if (string.IsNullOrEmpty(organizationIdParam) || !Guid.TryParse(organizationIdParam, out var organizationId))
            {
                // Connection lost before / without organization id — nothing actionable to clean up.
                await base.OnDisconnectedAsync(exception);
                return;
            }

            var connection = await connectionRepository.GetBySignalRConnectionIdAsync(Context.ConnectionId, organizationId);
            if (connection != null)
            {
                await connectionRepository.DeleteConnection(
                    connection.OrganizationId,
                    connection.AgentId,
                    connection.InstanceName);

                await _bus.Publish(new AgentAvailabilityChangedEvent
                {
                    AgentId = connection.AgentId,
                    AgentInstanceName = connection.InstanceName
                });

                _logger.LogInformation(
                    "Agent '{InstanceName}' disconnected from agent {AgentId} in organization {OrganizationId}",
                    connection.InstanceName, connection.AgentId, connection.OrganizationId);
            }

            // Park this connection's in-flight runs as AwaitingReconnect with a grace window, rather
            // than failing them: the orchestrator→sidecar stream is independent of the hub, so the
            // agent may still be running. A resumed heartbeat flips them back to Running; if the grace
            // lapses with no heartbeat, the deadline-check recovers them.
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var graceDeadline = DateTime.UtcNow.Add(ReconnectGrace);
            var parked = await db.ModuleJobMissionRuns
                .Where(r => r.SignalRConnectionId == Context.ConnectionId
                            && r.OrganizationId == organizationId
                            && (r.Status == MissionStatus.Pending || r.Status == MissionStatus.Running))
                .ToListAsync();
            foreach (var run in parked)
            {
                run.Status = MissionStatus.AwaitingReconnect;
                if (run.DeadlineAt < graceDeadline) run.DeadlineAt = graceDeadline;
            }
            if (parked.Count > 0)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation("Parked {Count} in-flight run(s) AwaitingReconnect for connection {ConnectionId}.",
                    parked.Count, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in agent disconnection for {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
