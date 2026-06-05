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
/// Layer 1 for job-cancellation triggers — also fires <see cref="MissionType.AutoDiagnose"/>. Cancels
/// include declined approvals (no terraform error in the logs; the "failure" is policy/review), so
/// AutoDiagnose reads the approvals MCP resource to surface the decline reason and what to fix.
/// </summary>
public class ApplyJobCancelledCompetingConsumer :
    IConsumer<ApplyModuleCancelled>,
    IConsumer<DestroyModuleCancelled>
{
    private const MissionType TriggeredType = MissionType.AutoDiagnose;

    private readonly MissionMatcher _matcher;

    public ApplyJobCancelledCompetingConsumer(MissionMatcher matcher) => _matcher = matcher;

    public Task Consume(ConsumeContext<ApplyModuleCancelled> context) =>
        _matcher.MatchAndDispatchAsync(context.Message.ModuleId, context.Message.OrganizationId,
            context.Message.ModuleJobId, TriggeredType, context.CancellationToken);

    public Task Consume(ConsumeContext<DestroyModuleCancelled> context) =>
        _matcher.MatchAndDispatchAsync(context.Message.ModuleId, context.Message.OrganizationId,
            context.Message.ModuleJobId, TriggeredType, context.CancellationToken);
}
