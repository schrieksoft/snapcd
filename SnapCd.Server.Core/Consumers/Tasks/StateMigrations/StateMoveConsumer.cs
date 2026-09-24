// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests.StateMigrations;
using SnapCd.Server.Core.Consumers.Tasks.Builders;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.StateMigrations;

/// <summary>Sends a batch of addresses to move, import or remove to the runner.</summary>
public class StateMoveConsumer : IConsumer<StateMoveRequested>
{
    private readonly ILogger<StateMoveConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    public StateMoveConsumer(
        ILogger<StateMoveConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    public async Task Consume(ConsumeContext<StateMoveRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        try
        {
            var runner = await _runnerSelection.SelectSpecificRunnerAsync(
                msg.OrganizationId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException($"No available runners in pool {msg.RunnerId}");

            var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, []);

            var instructions = msg.Instructions
                .Select(i => new StateAddressInstruction { Address = i.Address, Target = i.Target })
                .ToList();

            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                EndpointFor(msg.Operation),
                new StateMoveRequestBase
                {
                    JobId = jobId,
                    OrganizationId = msg.OrganizationId,
                    Metadata = ordinary.Metadata,
                    Engine = ordinary.Engine,
                    Operation = msg.Operation.ToString(),
                    Instructions = instructions
                });

            _logger.LogDebug(
                "Dispatched {Operation} to runner {RunnerName} for job {JobId}",
                msg.Operation, runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching {Operation} for job {JobId}", msg.Operation, jobId);

            await context.Publish(new StateMoveFaulted
            {
                CorrelationId = jobId,
                OrganizationId = msg.OrganizationId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }

    private static string EndpointFor(AddressOperation operation) => operation switch
    {
        AddressOperation.Mv => RunnerEndpoints.StateMv,
        AddressOperation.Import => RunnerEndpoints.StateImport,
        AddressOperation.Remove => RunnerEndpoints.StateRemove,
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
