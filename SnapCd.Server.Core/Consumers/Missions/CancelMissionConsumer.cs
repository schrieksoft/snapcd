// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.AgentRequests;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Hubs;

namespace SnapCd.Server.Core.Consumers.Missions;

/// <summary>
/// Layer 2 for cancellation: runs on the instance that owns the agent's connection (Layer 1
/// directed-Sent here) and pushes <see cref="AgentEndpoints.CancelMission"/> to it — the agent twin
/// of a runner cancel. The orchestrator cancels that run's token; confirmation arrives via
/// <c>AgentHub.MissionCancelled</c>.
/// </summary>
public class CancelMissionConsumer : IConsumer<CancelMissionRunRequested>
{
    private readonly IHubContext<AgentHub> _hub;
    private readonly ILogger<CancelMissionConsumer> _logger;

    public CancelMissionConsumer(IHubContext<AgentHub> hub, ILogger<CancelMissionConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CancelMissionRunRequested> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Cancelling mission run {RunId} on agent connection {ConnectionId}",
            msg.RunId, msg.AgentConnectionId);

        await _hub.Clients.Client(msg.AgentConnectionId).SendAsync(
            AgentEndpoints.CancelMission,
            new CancelMissionRequest { InvocationId = msg.InvocationId, RunId = msg.RunId },
            context.CancellationToken);
    }
}
