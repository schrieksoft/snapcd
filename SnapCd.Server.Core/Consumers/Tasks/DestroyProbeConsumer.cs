// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Services.Nonsecured.RippleProbes;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Answers the probe step for a destroy, on the server, without a runner.
///
/// A destroy takes this Module's outputs to null. There is nothing to plan and no value to
/// predict, so the answer is known here: the edge this Module owes the wave is marked
/// ModuleDestroyed and the step completes.
/// </summary>
public class DestroyProbeConsumer : IConsumer<DestroyProbeRequested>
{
    private readonly ILogger<DestroyProbeConsumer> _logger;
    private readonly SignalEdgeResolverFactory _resolverFactory;
    private readonly ProbeSignalRecorderFactory _recorderFactory;

    public DestroyProbeConsumer(
        ILogger<DestroyProbeConsumer> logger,
        SignalEdgeResolverFactory resolverFactory,
        ProbeSignalRecorderFactory recorderFactory)
    {
        _logger = logger;
        _resolverFactory = resolverFactory;
        _recorderFactory = recorderFactory;
    }

    public async Task Consume(ConsumeContext<DestroyProbeRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;
        var orgId = msg.OrganizationId;
        var moduleId = msg.Declared.ModuleId;

        try
        {
            if (msg.ProbeWaveId != Guid.Empty)
            {
                // The attempt is marked answered as well as the edge resolved: the saga asks
                // whether another wave is owed by reading attempts, and an unrecorded answer
                // sends it round the probe branch again.
                using (var recorder = _recorderFactory.Create())
                    await recorder.RecordEmptyReport(msg.ProbeWaveId, moduleId, orgId, jobId);

                using var resolver = _resolverFactory.Create();
                await resolver.ResolveAsModuleDestroyed(msg.ProbeWaveId, moduleId, orgId);
            }

            await context.Publish(new DestroyProbeCompleted
            {
                CorrelationId = jobId,
                OrganizationId = orgId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error answering DestroyProbe for job {JobId}", jobId);
            await context.Publish(new DestroyProbeFaulted
            {
                CorrelationId = jobId,
                OrganizationId = orgId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}
