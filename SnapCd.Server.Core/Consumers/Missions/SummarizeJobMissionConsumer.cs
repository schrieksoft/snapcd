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
/// Layer 2 for SummarizeJob — runs on the instance that owns the agent's connection and pushes
/// <see cref="AgentEndpoints.SummarizeJob"/> with the typed <see cref="SummarizeJobRequest"/>.
/// </summary>
public class SummarizeJobMissionConsumer : IConsumer<SummarizeJobMissionRequested>
{
    private readonly IHubContext<AgentHub> _hub;
    private readonly ILogger<SummarizeJobMissionConsumer> _logger;

    public SummarizeJobMissionConsumer(IHubContext<AgentHub> hub, ILogger<SummarizeJobMissionConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SummarizeJobMissionRequested> context)
    {
        var msg = context.Message;

        _logger.LogDebug("Invoking SummarizeJob mission {MissionId} on agent connection {ConnectionId}",
            msg.MissionId, msg.AgentConnectionId);

        await _hub.Clients.Client(msg.AgentConnectionId).SendAsync(
            AgentEndpoints.SummarizeJob,
            new SummarizeJobRequest
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
