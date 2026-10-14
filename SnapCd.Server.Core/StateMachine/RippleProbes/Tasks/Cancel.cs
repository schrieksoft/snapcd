// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.RippleProbe;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.RippleProbes;

public partial class RippleProbeStateMachine
{
    public Request<RippleProbeSaga, RippleProbeCancelKillRequested, DummyRippleProbeCancelKillCompleted> CancelKillRequested { get; } = null!;
    public Event<RippleProbeCancelKillCompleted> CancelKillCompleted { get; } = null!;

    public Event<CancelRippleProbeJobRequested> CancelRippleProbeJobRequested { get; } = null!;

    public State CancellingImmediateKill { get; } = null!;
    public State CancellingAfterCurrent { get; } = null!;
    public State Cancelled { get; } = null!;

    private static readonly TimeSpan CancelRequestTimeout = TimeSpan.FromSeconds(90);

    private void Configure_Cancel()
    {
        Event(() => CancelRippleProbeJobRequested, x => x
            .CorrelateById(m => m.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(context => FinalizeWithoutSaga(context, context.Message.CorrelationId, context.Message.OrganizationId, nameof(CancelRippleProbeJobRequested)))));

        Request(() => CancelKillRequested, x => x.KillCancellationRequestId, o => { o.Timeout = CancelRequestTimeout; });

        Event(() => CancelKillCompleted, x => x
            .CorrelateById(y => y.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(context => FinalizeWithoutSaga(context, context.Message.CorrelationId, context.Message.OrganizationId, nameof(RippleProbeCancelKillCompleted)))));

        // A step that reports back while cancelling ends the job rather than continuing the chain.
        // The timeouts matter most when no step was ever dispatched: the runner has nothing to
        // kill and never answers, so without these the saga waits in Cancelling forever.
        // Cancelling again never re-issues the request: that would restart the clock, and repeated
        // clicks would push the deadline out indefinitely while orphaning each previous timeout. A
        // click once the timeout is overdue is instead evidence that it was lost, and forces the
        // job closed. Inside the window the click does nothing, because the timeout is still coming.
        During(CancellingImmediateKill,
            [
                .. CancelHandlers(),
                When(CancelRippleProbeJobRequested, TimeoutIsOverdue).ThenRippleProbeCancelForced(_logger, Cancelled),
                Ignore(CancelRippleProbeJobRequested),
                When(CancelKillRequested.TimeoutExpired).ThenRippleProbeCancelTimedOut(_logger, Cancelled)
            ]);

        During(CancellingAfterCurrent,
            [
                .. CancelHandlers(),
                When(CancelRippleProbeJobRequested, TimeoutIsOverdue).ThenRippleProbeCancelForced(_logger, Cancelled),
                Ignore(CancelRippleProbeJobRequested)
            ]);

        During(Cancelled,
            Ignore(CancelRippleProbeJobRequested),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }

    /// <summary>
    /// The cancel request this saga is waiting on should have timed out by now. A margin over the
    /// request's own timeout keeps a merely-late timeout from being treated as a lost one.
    /// </summary>
    private static bool TimeoutIsOverdue(BehaviorContext<RippleProbeSaga, CancelRippleProbeJobRequested> context) =>
        context.Saga.WaitingSince is { } since && DateTime.UtcNow - since > CancelRequestTimeout + TimeSpan.FromSeconds(15);

    /// <summary>
    /// Closes the job row when its saga is gone. Without this a cancel is silently swallowed and
    /// the row stays Running until the orphan sweep finds it, which reports it as abandoned rather
    /// than cancelled.
    /// </summary>
    private async Task FinalizeWithoutSaga(ConsumeContext context, Guid jobId, Guid organizationId, string eventName)
    {
        var serviceProvider = PipeExtensions.GetPayload<IServiceProvider>(context);
        var repository = serviceProvider.GetRequiredService<RippleProbeJobRepository>();
        var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

        _logger.LogWarning("Saga missing for {EventType} on ripple probe {JobId}, finalizing directly", eventName, jobId);

        // Published, so this reaches every saga: an id belonging to another job kind is not ours
        // to finalize. The repository throws rather than returning null.
        RippleProbeJob job;
        try
        {
            job = await repository.Get(jobId, organizationId);
        }
        catch (EntityNotFoundException)
        {
            return;
        }

        await publishEndpoint.Publish(new RippleProbeCancelled
        {
            ModuleId = job.ModuleId,
            OrganizationId = organizationId,
            ModuleJobId = jobId,
            CancellationReason = CancellationReason.UserRequested
        });

        await repository.Finalize(jobId, organizationId, ExecutionStatus.Cancelled, DateTimeOffset.UtcNow);
    }

    private EventActivities<RippleProbeSaga>[] CancelHandlers() =>
    [
        When(SelectRunnerInstanceCompleted).ThenRippleProbeCancelled(Cancelled),
        When(RippleProbeGetModuleCompleted).ThenRippleProbeCancelled(Cancelled),
        When(RippleProbeInitCompleted).ThenRippleProbeCancelled(Cancelled),
        When(RippleProbeValidateCompleted).ThenRippleProbeCancelled(Cancelled),
        When(CancelKillCompleted).ThenRippleProbeCancelled(Cancelled),
        Ignore(RunnerReconnectedEvent),
        Ignore(HeartbeatScheduled.Received),
        Ignore(HeartbeatRequested.Completed),
        Ignore(HeartbeatRequested.Completed2)
    ];
}
