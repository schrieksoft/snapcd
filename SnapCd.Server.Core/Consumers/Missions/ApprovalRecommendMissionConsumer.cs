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
/// Layer 2 per-mission dispatch consumer (see <see cref="AutoDiagnoseMissionConsumer"/>): invokes
/// the ApprovalRecommend endpoint on the agent connection co-located with this server instance.
/// </summary>
public class ApprovalRecommendMissionConsumer : IConsumer<ApprovalRecommendMissionRequested>
{
    private readonly IHubContext<AgentHub> _hub;
    private readonly ILogger<ApprovalRecommendMissionConsumer> _logger;

    public ApprovalRecommendMissionConsumer(IHubContext<AgentHub> hub, ILogger<ApprovalRecommendMissionConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ApprovalRecommendMissionRequested> context)
    {
        var msg = context.Message;

        _logger.LogInformation("Invoking ApprovalRecommend mission {MissionId} on agent connection {ConnectionId}",
            msg.MissionId, msg.AgentConnectionId);

        await _hub.Clients.Client(msg.AgentConnectionId).SendAsync(
            AgentEndpoints.ApprovalRecommend,
            new ApprovalRecommendRequest
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
