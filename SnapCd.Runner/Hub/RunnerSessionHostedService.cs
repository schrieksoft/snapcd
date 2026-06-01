// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Hub;

/// <summary>
/// Hosted service that manages the lifecycle of the RunnerHubConnection SignalR client
/// </summary>
public class RunnerSessionHostedService : IHostedService
{
    private readonly RunnerHubConnection _hubConnection;
    private readonly ILogger<RunnerSessionHostedService> _logger;

    public RunnerSessionHostedService(
        RunnerHubConnection hubConnection,
        ILogger<RunnerSessionHostedService> logger)
    {
        _hubConnection = hubConnection;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SignalR runner hub connection...");
        await _hubConnection.StartAsync(cancellationToken);
        _logger.LogInformation("SignalR runner hub connection succeeded");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SignalR runner hub connection...");
        await _hubConnection.StopAsync();
        _logger.LogInformation("SignalR runner hub connection stopped");
    }
}