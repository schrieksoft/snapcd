// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps.ManualJobs;

namespace SnapCd.Server.Core.Hubs.Handlers.ManualJobs;

/// <summary>
/// Publishes one manual family's reply to a setup step. Each family has its own handler because
/// each has its own messages: the endpoint the runner answered on decides which handler runs, so
/// nothing has to work out afterwards which saga was waiting.
/// </summary>
public abstract class ManualStepHandler<TCompleted, TCancelled, TFaulted>(ILogger logger, IBus bus)
    where TCompleted : ManualStepResponseBase, new()
    where TCancelled : ManualStepResponseBase, new()
    where TFaulted : ManualStepFaultedBase, new()
{
    protected abstract string Step { get; }

    public async Task Complete(Guid jobId, Guid organizationId, Action<TCompleted>? fill = null)
    {
        logger.LogDebug("Runner completed {Step} for job {JobId}", Step, jobId);

        var completed = new TCompleted { CorrelationId = jobId, OrganizationId = organizationId };
        fill?.Invoke(completed);

        await bus.Publish(completed);
    }

    public async Task Cancel(Guid jobId, Guid organizationId)
    {
        logger.LogDebug("Runner cancelled {Step} for job {JobId}", Step, jobId);

        await bus.Publish(new TCancelled { CorrelationId = jobId, OrganizationId = organizationId });
    }

    public async Task Fault(Guid jobId, Guid organizationId, string? errorMessage, string? stackTrace)
    {
        logger.LogError("Runner faulted {Step} for job {JobId}: {ErrorMessage}", Step, jobId, errorMessage);

        await bus.Publish(new TFaulted
        {
            CorrelationId = jobId,
            OrganizationId = organizationId,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }
}

// One handler per family per step: twelve names, each publishing exactly one family's reply.

public class StateListFilteredGetModuleHandler(ILogger<StateListFilteredGetModuleHandler> logger, IBus bus)
    : ManualStepHandler<StateListFilteredGetModuleCompleted, StateListFilteredGetModuleCancelled, StateListFilteredGetModuleFaulted>(logger, bus)
{
    protected override string Step => "GetModule";
}

public class StateListFilteredInitHandler(ILogger<StateListFilteredInitHandler> logger, IBus bus)
    : ManualStepHandler<StateListFilteredInitCompleted, StateListFilteredInitCancelled, StateListFilteredInitFaulted>(logger, bus)
{
    protected override string Step => "Init";
}

public class MoveGetModuleHandler(ILogger<MoveGetModuleHandler> logger, IBus bus)
    : ManualStepHandler<MoveGetModuleCompleted, MoveGetModuleCancelled, MoveGetModuleFaulted>(logger, bus)
{
    protected override string Step => "GetModule";
}

public class MoveInitHandler(ILogger<MoveInitHandler> logger, IBus bus)
    : ManualStepHandler<MoveInitCompleted, MoveInitCancelled, MoveInitFaulted>(logger, bus)
{
    protected override string Step => "Init";
}

public class ImportGetModuleHandler(ILogger<ImportGetModuleHandler> logger, IBus bus)
    : ManualStepHandler<ImportGetModuleCompleted, ImportGetModuleCancelled, ImportGetModuleFaulted>(logger, bus)
{
    protected override string Step => "GetModule";
}

public class ImportInitHandler(ILogger<ImportInitHandler> logger, IBus bus)
    : ManualStepHandler<ImportInitCompleted, ImportInitCancelled, ImportInitFaulted>(logger, bus)
{
    protected override string Step => "Init";
}

public class RemoveGetModuleHandler(ILogger<RemoveGetModuleHandler> logger, IBus bus)
    : ManualStepHandler<RemoveGetModuleCompleted, RemoveGetModuleCancelled, RemoveGetModuleFaulted>(logger, bus)
{
    protected override string Step => "GetModule";
}

public class RemoveInitHandler(ILogger<RemoveInitHandler> logger, IBus bus)
    : ManualStepHandler<RemoveInitCompleted, RemoveInitCancelled, RemoveInitFaulted>(logger, bus)
{
    protected override string Step => "Init";
}
