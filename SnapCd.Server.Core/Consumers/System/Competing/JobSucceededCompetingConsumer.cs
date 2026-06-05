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
/// Layer 1 for job-success triggers — fires <see cref="MissionType.SummarizeJob"/> on
/// <see cref="ApplyModuleCompleted"/> / <see cref="DestroyModuleCompleted"/>. The mission writes an
/// audit-quality summary of what changed, who approved, and what was anomalous.
/// </summary>
public class JobSucceededCompetingConsumer :
    IConsumer<ApplyModuleCompleted>,
    IConsumer<DestroyModuleCompleted>
{
    private const MissionType TriggeredType = MissionType.SummarizeJob;

    private readonly MissionMatcher _matcher;

    public JobSucceededCompetingConsumer(MissionMatcher matcher) => _matcher = matcher;

    public Task Consume(ConsumeContext<ApplyModuleCompleted> context) =>
        _matcher.MatchAndDispatchAsync(context.Message.ModuleId, context.Message.OrganizationId,
            context.Message.ModuleJobId, TriggeredType, context.CancellationToken);

    public Task Consume(ConsumeContext<DestroyModuleCompleted> context) =>
        _matcher.MatchAndDispatchAsync(context.Message.ModuleId, context.Message.OrganizationId,
            context.Message.ModuleJobId, TriggeredType, context.CancellationToken);
}
