// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using SnapCd.Agent.Configuration;
using SnapCd.Agent.Services;
using SnapCd.Contracts.AgentResults;

namespace SnapCd.Agent.Hub;

/// <summary>
/// SignalR client for the agent's connection to Snap CD Server's <c>/agenthub</c>. Owns the
/// connection lifecycle (auth, initial-connect retry, auto-reconnect) and hands the connection to
/// <see cref="Missions.Missions"/> to register the per-mission client endpoints. Mirrors
/// <c>SnapCd.Runner/Hub/RunnerHubConnection</c>.
/// </summary>
public sealed class AgentHubConnection : IAsyncDisposable
{
    private readonly AgentOptions _options;
    private readonly ServerSettings _server;
    private readonly TokenService _tokenService;
    private readonly Missions.Missions _missions;
    private readonly ILogger<AgentHubConnection> _logger;

    private HubConnection? _connection;
    private bool _stopping;

    // Buffered mission-log sending (mirrors RunnerHubConnection): streamed log lines forwarded to the
    // server are buffered if the connection is down / the send fails, and flushed on reconnect, so a
    // hub blip mid-mission doesn't lose log lines.
    private readonly ConcurrentQueue<BufferedLog> _logBuffer = new();
    private const int MaxLogBufferSize = 10000;
    private const int FlushChunkSize = 100;
    private int _droppedLogCount;

    private readonly record struct BufferedLog(Guid InvocationId, MissionLogLineDto Line);

    // Buffered mission-milestone sending — same robustness as logs. Milestones are important progress
    // checkpoints, so a hub blip mid-mission buffers them for flush on reconnect rather than losing them.
    private readonly ConcurrentQueue<BufferedMilestone> _milestoneBuffer = new();
    private readonly record struct BufferedMilestone(Guid InvocationId, MissionMilestoneDto Milestone);

    public AgentHubConnection(
        IOptions<AgentOptions> options,
        IOptions<ServerSettings> server,
        TokenService tokenService,
        Missions.Missions missions,
        ILogger<AgentHubConnection> logger)
    {
        _options = options.Value;
        _server = server.Value;
        _tokenService = tokenService;
        _missions = missions;
        _logger = logger;
    }

    /// <summary>Build the connection, register the mission endpoints, and connect (retrying until the server is reachable).</summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection != null)
        {
            _logger.LogWarning("Agent hub connection already established");
            return;
        }

        var hubUrl = $"{_server.Url.TrimEnd('/')}/agenthub" +
                     $"?organization_id={_options.OrganizationId}&agent_id={_options.AgentId}" +
                     (string.IsNullOrEmpty(_options.InstanceName) ? "" : $"&agent_instance={Uri.EscapeDataString(_options.InstanceName)}");

        _logger.LogInformation("Connecting to agent hub at {HubUrl}", hubUrl);

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () => await _tokenService.GetCurrentTokenAsync(cancellationToken);
            })
            .WithAutomaticReconnect(new RetryPolicy())
            .Build();

        _connection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "Agent hub connection lost; reconnecting...");
            return Task.CompletedTask;
        };
        _connection.Reconnected += async connectionId =>
        {
            _logger.LogInformation("Agent hub reconnected with connection ID {ConnectionId}", connectionId);
            await FlushLogBufferAsync();
            await FlushMilestoneBufferAsync();
        };
        _connection.Closed += error =>
        {
            if (!_stopping)
                _logger.LogError(error, "Agent hub connection closed unexpectedly");
            return Task.CompletedTask;
        };

        _missions.RegisterHandlers(_connection, this, cancellationToken);

        await ConnectWithRetryAsync(cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
            await FlushLogBufferAsync();
            await FlushMilestoneBufferAsync();
            _logger.LogInformation("Agent connected to {HubUrl}; awaiting mission invocations.", hubUrl);
        }
    }

    /// <summary>Send a batch of streamed mission log lines to the server, buffering on failure/disconnect.</summary>
    public async Task SendMissionLogsAsync(Guid invocationId, IReadOnlyList<MissionLogLineDto> lines)
    {
        if (lines.Count == 0)
            return;

        if (_connection?.State != HubConnectionState.Connected)
        {
            BufferLines(invocationId, lines);
            return;
        }

        try
        {
            await _connection.InvokeAsync("AddMissionLogs", invocationId, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mission logs for {InvocationId}; buffering for retry", invocationId);
            BufferLines(invocationId, lines);
        }
    }

    private void BufferLines(Guid invocationId, IReadOnlyList<MissionLogLineDto> lines)
    {
        foreach (var line in lines)
        {
            if (_logBuffer.Count >= MaxLogBufferSize)
            {
                _logBuffer.TryDequeue(out _);
                _droppedLogCount++;
            }
            _logBuffer.Enqueue(new BufferedLog(invocationId, line));
        }

        if (_droppedLogCount > 0)
            _logger.LogWarning("Dropped {Count} mission log entries due to buffer overflow", _droppedLogCount);
    }

    private async Task FlushLogBufferAsync()
    {
        if (_logBuffer.IsEmpty || _connection?.State != HubConnectionState.Connected)
            return;

        var drained = new List<BufferedLog>();
        while (drained.Count < FlushChunkSize && _logBuffer.TryDequeue(out var log))
            drained.Add(log);

        // Preserve order per mission; one AddMissionLogs call per invocation in the drained chunk.
        foreach (var group in drained.GroupBy(b => b.InvocationId))
        {
            var lines = group.Select(b => b.Line).ToList();
            try
            {
                await _connection!.InvokeAsync("AddMissionLogs", group.Key, lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing buffered mission logs for {InvocationId}; re-queuing", group.Key);
                foreach (var buffered in group)
                    _logBuffer.Enqueue(buffered);
            }
        }

        if (_droppedLogCount > 0)
        {
            _logger.LogWarning("Total of {Count} mission log entries were dropped due to buffer overflow", _droppedLogCount);
            _droppedLogCount = 0;
        }
    }

    /// <summary>Send one streamed mission milestone to the server, buffering on failure/disconnect.</summary>
    public async Task SendMissionMilestoneAsync(Guid invocationId, MissionMilestoneDto milestone)
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            BufferMilestone(invocationId, milestone);
            return;
        }

        try
        {
            await _connection.InvokeAsync("AddMissionMilestone", invocationId, milestone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mission milestone for {InvocationId}; buffering for retry", invocationId);
            BufferMilestone(invocationId, milestone);
        }
    }

    private void BufferMilestone(Guid invocationId, MissionMilestoneDto milestone)
    {
        if (_milestoneBuffer.Count >= MaxLogBufferSize)
            _milestoneBuffer.TryDequeue(out _);
        _milestoneBuffer.Enqueue(new BufferedMilestone(invocationId, milestone));
    }

    private async Task FlushMilestoneBufferAsync()
    {
        if (_milestoneBuffer.IsEmpty || _connection?.State != HubConnectionState.Connected)
            return;

        var drained = new List<BufferedMilestone>();
        while (drained.Count < FlushChunkSize && _milestoneBuffer.TryDequeue(out var m))
            drained.Add(m);

        // Send individually, preserving order — milestones are low-volume.
        foreach (var buffered in drained)
        {
            try
            {
                await _connection!.InvokeAsync("AddMissionMilestone", buffered.InvocationId, buffered.Milestone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing buffered milestone for {InvocationId}; re-queuing", buffered.InvocationId);
                _milestoneBuffer.Enqueue(buffered);
            }
        }
    }

    /// <summary>Stop and dispose the connection.</summary>
    public async Task StopAsync()
    {
        _stopping = true;
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
        _logger.LogInformation("Disconnected from agent hub");
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    /// <summary>
    /// Initial connect with capped backoff. <see cref="HubConnectionBuilder.WithAutomaticReconnect()"/>
    /// only retries *after* a successful connection drops, so the first connect needs its own retry —
    /// the server may not be up yet. Never throws: a startup failure must not stop the host.
    /// </summary>
    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(2);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _connection!.StartAsync(cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Agent hub connect failed; retrying in {Delay}s. " +
                    "Check Server:Url, Agent:ClientId/ClientSecret and that the server is reachable.",
                    delay.TotalSeconds);

                try { await Task.Delay(delay, cancellationToken); }
                catch (OperationCanceledException) { return; }
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }
    }

    /// <summary>Custom retry policy for SignalR auto-reconnect — keeps retrying indefinitely.</summary>
    private sealed class RetryPolicy : IRetryPolicy
    {
        private readonly TimeSpan[] _retryDelays =
        [
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20)
        ];

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
            => retryContext.PreviousRetryCount < (uint)_retryDelays.Length
                ? _retryDelays[retryContext.PreviousRetryCount]
                : _retryDelays[^1];
    }
}
