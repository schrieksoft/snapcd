// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Services.RippleProbes;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

/// <summary>
/// Starts a wave from the outputs this plan says will change, when anything downstream reads one
/// of them.
///
/// A failure here does not fail the job: the apply is still a legitimate apply, and losing the
/// propagation costs information about its consequences rather than correctness.
/// </summary>
public class StartProbeWaveActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase, SagaStateMachineInstance
    where TMessage : class
{
    private readonly ProbeWaveStarter? _starter;
    private readonly ModuleJobRepository? _moduleJobs;
    private readonly ILogger<StartProbeWaveActivity<TSaga, TMessage>> _logger;

    /// <summary>
    /// The starter is optional so that a host which has not wired propagation still applies. The
    /// activity sits on the main apply path, and a missing dependency there must not stop a job
    /// from running.
    /// </summary>
    public StartProbeWaveActivity(
        ILogger<StartProbeWaveActivity<TSaga, TMessage>> logger,
        ProbeWaveStarter? starter = null,
        ModuleJobRepository? moduleJobs = null)
    {
        _starter = starter;
        _moduleJobs = moduleJobs;
        _logger = logger;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        try
        {
            if (_starter != null && context.Message is PlanCompletedBase plan)
            {
                var changed = ChangedOutputNames(plan);

                if (changed.Count > 0)
                {
                    await _starter.Start(
                        context.Saga.ModuleId,
                        context.Saga.OrganizationId,
                        context.Saga.CorrelationId,
                        changed,
                        plan.OutputSignalsJson);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Could not start propagation for job {CorrelationId}; the apply is unaffected",
                context.Saga.CorrelationId);

            // Recorded on the row rather than only logged: an approver deciding without the
            // downstream answers should be told they are missing, not left to infer it from a
            // panel that shows nothing.
            await RecordFailure(context, ex);
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    private async Task RecordFailure(BehaviorContext<TSaga, TMessage> context, Exception ex)
    {
        try
        {
            if (_moduleJobs == null) return;

            var job = await _moduleJobs.Get(context.Saga.CorrelationId, context.Saga.OrganizationId);
            job.PropagationError = Truncate(ex.Message, 1000);
            await _moduleJobs.ExecuteUpdate(job);
        }
        catch (Exception recordingFailed)
        {
            _logger.LogError(recordingFailed,
                "Could not record the propagation failure for job {CorrelationId}",
                context.Saga.CorrelationId);
        }
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];

    /// <summary>
    /// The outputs whose value changes for someone already reading it. Unchanged outputs carry no
    /// signal, and a destroyed output is a change a consumer needs to hear about as much as a
    /// modified one. A created output is not: it has no value yet for anyone to have consumed.
    /// </summary>
    private static List<string> ChangedOutputNames(PlanCompletedBase plan)
    {
        return Split(plan.OutputsModifyList)
            .Concat(Split(plan.OutputsDestroyList))
            .Concat(Split(plan.OutputsRecreateList))
            .Distinct()
            .ToList();
    }

    private static IEnumerable<string> Split(string? list) =>
        string.IsNullOrWhiteSpace(list)
            ? []
            : list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("start-probe-wave");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
