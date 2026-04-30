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