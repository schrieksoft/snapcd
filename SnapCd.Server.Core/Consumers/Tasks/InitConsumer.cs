using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives Init requests and dispatches them to runners via SignalR.
/// Now resolves environment variables on the server before dispatching to eliminate circular API calls.
/// </summary>
public class InitConsumer : IConsumer<InitRequested>
{
    private readonly ILogger<InitConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;
    private readonly ParamResolverFactory _paramResolverFactory;

    public InitConsumer(
        ILogger<InitConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection,
        ParamResolverFactory paramResolverFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
        _paramResolverFactory = paramResolverFactory;
    }

    public async Task Consume(ConsumeContext<InitRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        var orgId = msg.OrganizationId;
        var runnerId = msg.RunnerId;
        var specificRunner = msg.RunnerInstanceName;

        _logger.LogInformation("Received Init request for job {JobId} in pool {RunnerId}",
            jobId, runnerId);

        try
        {
            // Resolve environment variables on the server before dispatching
            _logger.LogInformation("Resolving environment variables for job {JobId}", jobId);

            var metadata = new JobMetadata
            {
                ModuleName = msg.Declared.ModuleName,
                NamespaceName = msg.Declared.NamespaceName,
                StackName = msg.Declared.StackName,
                ModuleId = msg.Declared.ModuleId,
                SourceSubdirectory = msg.Declared.SourceSubdirectory
            };
                
            var taskContext = new ServerTaskContext(
                jobId,
                "Init",
                _logger,
                metadata
            );

            var paramResolver = _paramResolverFactory.CreateForEnvVars(
                taskContext,
                msg.Declared.ModuleEnvVarFromDefinitions,
                msg.Declared.ModuleEnvVarFromLiterals,
                msg.Declared.ModuleEnvVarFromNamespaces,
                msg.Declared.NamespaceEnvVarFromLiterals,
                msg.Declared.NamespaceEnvVarFromDefinitions,
                msg.Declared.SelectedModuleEnvVarsFromSecrets,
                msg.Declared.SelectedNamespaceEnvVarsFromSecrets,
                msg.Declared.StackId,
                msg.Declared.StackName,
                msg.Declared.NamespaceId,
                msg.Declared.NamespaceName,
                msg.Declared.ModuleId,
                msg.Declared.ModuleName,
                msg.Declared.SourceRevision,
                msg.Declared.SourceUrl,
                msg.Declared.SourceSubdirectory,
                orgId,
                msg.Declared.Engine
            );

            var resolvedEnvVars = await paramResolver.ResolveEnvVariables();

            _logger.LogInformation("Resolved {Count} environment variables for job {JobId}",
                resolvedEnvVars.Count, jobId);

            // Select runner using least-loaded strategy
            var runner = await _runnerSelection.SelectSpecificRunnerAsync(orgId, runnerId, specificRunner);

            if (runner == null)
            {
                _logger.LogWarning("No available runners in pool {RunnerId} for job {JobId}", runnerId, jobId);
                throw new InvalidOperationException($"No available runners in pool {runnerId}");
            }

            _logger.LogInformation("Selected runner {RunnerName} (ConnectionId: {ConnectionId}) for job {JobId}",
                runner.InstanceName, runner.SignalRConnectionId, jobId);

            // Invoke method on specific runner via SignalR
            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.Init,
                new InitRequestBase
                {
                    JobId = jobId,
                    OrganizationId = orgId,
                    Metadata = metadata,
                    Engine = msg.Declared.Engine,
                    InitBeforeHook = msg.Declared.InitBeforeHook,
                    InitAfterHook = msg.Declared.InitAfterHook,
                    BackendConfiguration = new EngineBackendConfiguration
                    {
                        IgnoreNamespaceBackendConfigs = msg.Declared.IgnoreNamespaceBackendConfigs,
                        NamespaceBackendConfigs = msg.Declared.NamespaceBackendConfigs,
                        ModuleBackendConfigs = msg.Declared.ModuleBackendConfigs,
                        PulumiFlags = msg.Declared.PulumiFlags
                            .Where(f => f.Task == PulumiCommandTask.Init)
                            .ToList(),
                        PulumiArrayFlags = msg.Declared.PulumiArrayFlags
                            .Where(f => f.Task == PulumiCommandTask.Init)
                            .ToList(),
                        TerraformFlags = msg.Declared.TerraformFlags
                            .Where(f => f.Task == TerraformCommandTask.Init)
                            .ToList(),
                        TerraformArrayFlags = msg.Declared.TerraformArrayFlags
                            .Where(f => f.Task == TerraformCommandTask.Init)
                            .ToList()
                    },
                    Flags = new EngineFlags
                    {
                        AutoUpgradeEnabled = msg.Declared.AutoUpgradeEnabled,
                        AutoReconfigureEnabled = msg.Declared.AutoReconfigureEnabled,
                        AutoMigrateEnabled = msg.Declared.AutoMigrateEnabled
                    },
                    CleanInitEnabled = msg.Declared.CleanInitEnabled,
                    ResolvedEnvVars = resolvedEnvVars
                }
            );

            _logger.LogInformation("Dispatched Init request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching Init request for job {JobId}", jobId);
            await context.Publish(new InitFaulted
            {
                CorrelationId = jobId,
                OrganizationId = orgId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}