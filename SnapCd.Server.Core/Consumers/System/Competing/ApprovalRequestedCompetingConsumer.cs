// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Services.Ai.Missions;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Layer 1 for awaiting-approval triggers — fires <see cref="MissionType.ApprovalRecommend"/> on
/// <see cref="ModuleJobAwaitingApprovalEvent"/> (published by
/// <c>WaitingForApprovalModuleJobActivity</c> when a job enters the awaiting-approval state).
/// </summary>
public class ApprovalRequestedCompetingConsumer : IConsumer<ModuleJobAwaitingApprovalEvent>
{
    private const MissionType TriggeredType = MissionType.ApprovalRecommend;

    private readonly MissionMatcher _matcher;

    public ApprovalRequestedCompetingConsumer(MissionMatcher matcher) => _matcher = matcher;

    public Task Consume(ConsumeContext<ModuleJobAwaitingApprovalEvent> context) =>
        _matcher.MatchAndDispatchAsync(context.Message.ModuleId, context.Message.OrganizationId,
            context.Message.ModuleJobId, TriggeredType, context.CancellationToken);
}
