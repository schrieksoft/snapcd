// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Contracts.RunnerRequests.SplitMonolith;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.SplitMonolith;

public class MigrateRunConsumer : IConsumer<MigrateRunRequested>
{
    private readonly ILogger<MigrateRunConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    public MigrateRunConsumer(
        ILogger<MigrateRunConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    public async Task Consume(ConsumeContext<MigrateRunRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;
        var orgId = msg.OrganizationId;

        try
        {
            var metadata = new JobMetadata
            {
                ModuleName = msg.Declared.ModuleName,
                NamespaceName = msg.Declared.NamespaceName,
                StackName = msg.Declared.StackName,
                ModuleId = msg.Declared.ModuleId,
                SourceSubdirectory = msg.Declared.SourceSubdirectory
            };

            var runner = await _runnerSelection.SelectSpecificRunnerAsync(orgId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
            {
                _logger.LogWarning("No available runners in pool {RunnerId} for job {JobId}", msg.RunnerId, jobId);
                throw new InvalidOperationException($"No available runners in pool {msg.RunnerId}");
            }

            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.MigrateRun,
                new MigrateRunRequestBase
                {
                    JobId = jobId,
                    OrganizationId = orgId,
                    Metadata = metadata,
                    Engine = msg.Declared.Engine,
                    RootDirectory = msg.RootDirectory,
                    // The same array flags the Init step turns into -backend-config, so a backend
                    // needing settings outside its block is configured identically here.
                    BackendConfigs = msg.Declared.TerraformArrayFlags
                        .Where(f => f.Task == TerraformCommandTask.Init
                                    && f.Flag == TerraformArrayFlag.BackendConfig)
                        .Select(f => f.Value)
                        .ToList(),
                    Overwrite = msg.Overwrite,
                }
            );

            _logger.LogDebug("Dispatched MigrateRun request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching MigrateRun request for job {JobId}", jobId);
            await context.Publish(new MigrateRunFaulted
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
