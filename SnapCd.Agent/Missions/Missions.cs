// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Agent.Hub;
using SnapCd.Agent.Models;
using SnapCd.Agent.Services;
using SnapCd.Agent.Services.Sidecars;
using SnapCd.Contracts.AgentRequests;
using SnapCd.Contracts.AgentResults;
using SnapCd.Contracts.Constants;

namespace SnapCd.Agent.Missions;

/// <summary>
/// The agent's hub client endpoints — one method per mission (one file each), mirroring
/// <c>SnapCd.Runner/Tasks</c>. <see cref="RegisterHandlers"/> wires each <see cref="AgentEndpoints"/>
/// method to its handler; each handler forwards to the named sidecar and streams the run back to the
/// server (<c>AddMissionLogs</c> + <c>MissionCompleted</c>/<c>MissionFaulted</c>).
/// </summary>
public sealed partial class Missions
{
    // Bound concurrent sidecar invocations so a burst of triggers can't exhaust the host.
    private const int MaxConcurrentInvocations = 8;

    // Flush streamed log lines to the server in batches of this size.
    private const int LogBatchSize = 10;

    private readonly SidecarRegistry _sidecars;
    private readonly TokenService _tokenService;
    private readonly ILogger<Missions> _logger;
    private readonly SemaphoreSlim _concurrency = new(MaxConcurrentInvocations);

    // Dedicated liveness ping cadence — well under AgentHub.ServerTimeout so a few misses don't
    // trip the watchdog. Fires regardless of sidecar output (the ReportRunningTask twin).
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    // In-flight runs keyed by InvocationId, so an inbound CancelMission cancels the right run's token.
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runs = new();

    // Set once at RegisterHandlers time; owns the buffered log-send path. Passed in (not ctor-injected)
    // to avoid a DI cycle with AgentHubConnection, which constructs Missions.
    private AgentHubConnection _hub = null!;

    public Missions(SidecarRegistry sidecars, TokenService tokenService, ILogger<Missions> logger)
    {
        _sidecars = sidecars;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Registers one client handler per <see cref="AgentEndpoints"/> method on the connection.</summary>
    public void RegisterHandlers(HubConnection connection, AgentHubConnection hub, CancellationToken ct)
    {
        _hub = hub;

        connection.On<AutoDiagnoseRequest>(AgentEndpoints.AutoDiagnose, req => AutoDiagnose(req, connection, ct));
        connection.On<ApprovalRecommendRequest>(AgentEndpoints.ApprovalRecommend, req => ApprovalRecommend(req, connection, ct));
        connection.On<SummarizeJobRequest>(AgentEndpoints.SummarizeJob, req => SummarizeJob(req, connection, ct));
        connection.On<CancelMissionRequest>(AgentEndpoints.CancelMission, CancelRun);
    }

    /// <summary>Inbound cancel: cancel the matching run's token, aborting its sidecar invoke.</summary>
    private void CancelRun(CancelMissionRequest req)
    {
        if (_runs.TryGetValue(req.InvocationId, out var cts))
        {
            _logger.LogInformation("Cancel requested for run {Invocation}.", req.InvocationId);
            try { cts.Cancel(); } catch (ObjectDisposedException) { /* already settled */ }
        }
    }

    /// <summary>Fire-and-forget: never block the hub receive loop on a long-running agent run.</summary>
    private void Dispatch(HubConnection connection, MissionRequestBase req, string missionName, string skillName, string sessionMode, Dictionary<string, string> parameters, CancellationToken ct)
        => _ = DispatchAsync(connection, req, missionName, skillName, sessionMode, parameters, ct);

    /// <summary>
    /// Shared per-mission flow: forwards to the sidecar, streams its log lines back via
    /// <c>AddMissionLogs</c>, and reports the outcome via <c>MissionCompleted</c>/<c>MissionFaulted</c>.
    /// </summary>
    private async Task DispatchAsync(HubConnection connection, MissionRequestBase req, string missionName, string skillName, string sessionMode, Dictionary<string, string> parameters, CancellationToken ct)
    {
        IAgentSidecar? sidecar;
        if (req.SidecarName is null)
        {
            if (!_sidecars.TryGetSingle(out sidecar) || sidecar is null)
            {
                _logger.LogWarning("Mission {Mission} omitted SidecarName but agent has {Count} sidecars registered; cannot pick a default.",
                    missionName, _sidecars.All.Count);
                await TryInvokeAsync(connection, "MissionFaulted", ct, req.InvocationId, "NoDefaultSidecar", $"agent has {_sidecars.All.Count} sidecars");
                return;
            }
        }
        else if (!_sidecars.TryGet(req.SidecarName, out sidecar) || sidecar is null)
        {
            _logger.LogWarning("Mission {Mission} references unknown sidecar '{Sidecar}'; skipping.", missionName, req.SidecarName);
            await TryInvokeAsync(connection, "MissionFaulted", ct, req.InvocationId, "UnknownSidecar", req.SidecarName);
            return;
        }

        await _concurrency.WaitAsync(ct);

        // Per-run cancellation: an inbound CancelMission cancels this token, aborting the sidecar stream.
        var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runs[req.InvocationId] = runCts;
        var runCt = runCts.Token;

        var logBatch = new List<MissionLogLineDto>();
        var progress = new ProgressClock();
        using var heartbeatCts = new CancellationTokenSource();
        var heartbeat = HeartbeatLoopAsync(connection, req.InvocationId, progress, heartbeatCts.Token);
        try
        {
            await connection.InvokeAsync("MissionStarted", req.InvocationId, runCt);

            // Make the org available to the skill (org-scoped MCP resource URIs need it).
            parameters["organizationId"] = req.OrganizationId.ToString();

            var token = await _tokenService.GetCurrentTokenAsync(runCt);
            var invoke = new InvokeRequest
            {
                Mission = missionName,
                Skill = skillName,
                Parameters = parameters,
                Session = new SessionSpec { Mode = sessionMode, Key = req.MissionId.ToString() },
                SnapcdMcpToken = token,
                CorrelationId = req.InvocationId
            };

            MissionResultDto? result = null;
            await foreach (var ev in sidecar.InvokeStreamAsync(invoke, runCt))
            {
                progress.Mark(); // stream activity = progress (feeds the no-progress timeout)
                if (ev.IsResult)
                {
                    result = ev.Result;
                    continue;
                }

                logBatch.Add(new MissionLogLineDto { Timestamp = DateTimeOffset.UtcNow, Level = ev.Level, Message = ev.Message ?? string.Empty });
                if (logBatch.Count >= LogBatchSize)
                {
                    await _hub.SendMissionLogsAsync(req.InvocationId, logBatch);
                    logBatch = new List<MissionLogLineDto>();
                }
            }

            if (logBatch.Count > 0)
                await _hub.SendMissionLogsAsync(req.InvocationId, logBatch);

            if (result is not null)
                await connection.InvokeAsync("MissionCompleted", req.InvocationId, result, ct);
            else
                await connection.InvokeAsync("MissionFaulted", req.InvocationId, "NoResult", (string?)null, ct);

            _logger.LogInformation("Mission {Mission} ({Invocation}) → success={Success}",
                missionName, req.InvocationId, result?.Success);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { /* host shutting down */ }
        catch (OperationCanceledException) when (runCts.IsCancellationRequested)
        {
            _logger.LogInformation("Mission {Mission} ({Invocation}) cancelled.", missionName, req.InvocationId);
            await TryInvokeAsync(connection, "MissionCancelled", ct, req.InvocationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mission {Mission} ({Invocation}) dispatch to sidecar '{Sidecar}' failed.",
                missionName, req.InvocationId, sidecar.Name);
            await TryInvokeAsync(connection, "MissionFaulted", ct, req.InvocationId, ex.GetType().Name, ex.Message);
        }
        finally
        {
            heartbeatCts.Cancel();
            try { await heartbeat; } catch { /* heartbeat teardown is best-effort */ }
            _runs.TryRemove(req.InvocationId, out _);
            runCts.Dispose();
            _concurrency.Release();
        }
    }

    /// <summary>Dedicated periodic liveness ping — fires on a timer regardless of sidecar output,
    /// carrying the last-progress timestamp. Best-effort: a miss during a hub blip is fine (the server's
    /// reconnect grace + a resumed heartbeat cover it).</summary>
    private async Task HeartbeatLoopAsync(HubConnection connection, Guid invocationId, ProgressClock progress, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(HeartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try { await connection.InvokeAsync("MissionHeartbeat", invocationId, progress.Value, ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { _logger.LogDebug(ex, "Heartbeat for {Invocation} failed (will retry).", invocationId); }
            }
        }
        catch (OperationCanceledException) { /* run settled or shutting down */ }
    }

    /// <summary>Thread-safe last-progress timestamp shared between the dispatch loop and the heartbeat task.</summary>
    private sealed class ProgressClock
    {
        private long _ticks = DateTime.UtcNow.Ticks;
        public void Mark() => Interlocked.Exchange(ref _ticks, DateTime.UtcNow.Ticks);
        public DateTime Value => new(Interlocked.Read(ref _ticks), DateTimeKind.Utc);
    }

    /// <summary>Best-effort hub invoke — a reporting failure must not crash the dispatch loop.</summary>
    private async Task TryInvokeAsync(HubConnection connection, string method, CancellationToken ct, params object?[] args)
    {
        try { await connection.InvokeCoreAsync(method, args, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Hub callback {Method} failed.", method); }
    }
}
