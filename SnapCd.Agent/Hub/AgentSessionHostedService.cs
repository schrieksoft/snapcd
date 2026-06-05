// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Agent.Hub;

/// <summary>
/// Hosted service that manages the lifecycle of the <see cref="AgentHubConnection"/>. Mirrors
/// <c>SnapCd.Runner/Hub/RunnerSessionHostedService</c>, but kicks the connect off in the background
/// so a slow/absent server can't block host startup — the connection retries internally until it's up.
/// </summary>
public sealed class AgentSessionHostedService : IHostedService
{
    private readonly AgentHubConnection _hubConnection;
    private readonly ILogger<AgentSessionHostedService> _logger;

    private CancellationTokenSource? _cts;
    private Task? _connectTask;

    public AgentSessionHostedService(AgentHubConnection hubConnection, ILogger<AgentSessionHostedService> logger)
    {
        _hubConnection = hubConnection;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting agent hub connection...");
        _cts = new CancellationTokenSource();
        _connectTask = _hubConnection.StartAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping agent hub connection...");
        if (_cts != null)
            await _cts.CancelAsync();
        await _hubConnection.StopAsync();
    }
}
