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
using SnapCd.Contracts.AgentRequests;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Hubs;

namespace SnapCd.Server.Core.Consumers.Missions;

/// <summary>
/// Layer 2 (the agent's analogue of <c>Consumers/Tasks/PlanConsumer</c>): runs on the server
/// instance that owns the target agent's SignalR connection — Layer 1 directed-Sent here so this
/// consumer is co-located with the connection — and invokes the AutoDiagnose endpoint on it.
/// </summary>
public class AutoDiagnoseMissionConsumer : IConsumer<AutoDiagnoseMissionRequested>
{
    private readonly IHubContext<AgentHub> _hub;
    private readonly ILogger<AutoDiagnoseMissionConsumer> _logger;

    public AutoDiagnoseMissionConsumer(IHubContext<AgentHub> hub, ILogger<AutoDiagnoseMissionConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AutoDiagnoseMissionRequested> context)
    {
        var msg = context.Message;

        _logger.LogInformation("Invoking AutoDiagnose mission {MissionId} on agent connection {ConnectionId}",
            msg.MissionId, msg.AgentConnectionId);

        await _hub.Clients.Client(msg.AgentConnectionId).SendAsync(
            AgentEndpoints.AutoDiagnose,
            new AutoDiagnoseRequest
            {
                InvocationId = msg.InvocationId,
                RunId = msg.RunId,
                MissionId = msg.MissionId,
                OrganizationId = msg.OrganizationId,
                SidecarName = msg.SidecarName,
                JobId = msg.JobId,
                ModuleId = msg.ModuleId
            },
            context.CancellationToken);
    }
}
