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
/// Layer 1 of agent mission dispatch for job-failure triggers — delegates the scope-resolve + match
/// + live-first dispatch loop to <see cref="MissionMatcher"/>. On <see cref="ApplyModuleFailed"/> /
/// <see cref="DestroyModuleFailed"/> it prefers <see cref="MissionType.AutoFix"/>, which diagnoses
/// *and* remediates: if an AutoFix mission is configured for the job's scope, AutoFix runs and
/// <see cref="MissionType.AutoDiagnose"/> is suppressed as redundant; only when no AutoFix mission is
/// configured does AutoDiagnose run as the diagnose-only fallback.
/// </summary>
public class ApplyJobFailedCompetingConsumer :
    IConsumer<ApplyModuleFailed>,
    IConsumer<DestroyModuleFailed>
{
    private readonly MissionMatcher _matcher;

    public ApplyJobFailedCompetingConsumer(MissionMatcher matcher) => _matcher = matcher;

    public Task Consume(ConsumeContext<ApplyModuleFailed> context) =>
        DispatchAsync(context.Message.ModuleId, context.Message.OrganizationId,
            context.Message.ModuleJobId, context.CancellationToken);

    public Task Consume(ConsumeContext<DestroyModuleFailed> context) =>
        DispatchAsync(context.Message.ModuleId, context.Message.OrganizationId,
            context.Message.ModuleJobId, context.CancellationToken);

    private async Task DispatchAsync(Guid moduleId, Guid organizationId, Guid jobId, CancellationToken ct)
    {
        // AutoFix subsumes diagnosis, so it takes precedence: if an AutoFix mission is configured for
        // the scope it owns the failure (even if its agent is offline — it parks and wakes later), and
        // AutoDiagnose is suppressed. AutoDiagnose only fires as the fallback when no AutoFix is set up.
        var autoFixConfigured = await _matcher.MatchAndDispatchAsync(
            moduleId, organizationId, jobId, MissionType.AutoFix, ct);
        if (!autoFixConfigured)
            await _matcher.MatchAndDispatchAsync(
                moduleId, organizationId, jobId, MissionType.AutoDiagnose, ct);
    }
}
