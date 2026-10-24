// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Services.ModuleHold;

/// <summary>Parks a held module's queued work and keeps one hold check scheduled for it until the hold lapses.</summary>
public class ModuleHoldGate
{
    private readonly ModuleHoldStore _holds;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModuleHoldGate> _logger;

    public ModuleHoldGate(ModuleHoldStore holds, IServiceProvider serviceProvider, ILogger<ModuleHoldGate> logger)
    {
        _holds = holds;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // Resolved only when a module is held, so paths that never hold need no scheduler.
    private IMessageScheduler Scheduler => _serviceProvider.GetRequiredService<IMessageScheduler>();

    /// <summary>
    /// When the module is held, queues <paramref name="stateToQueue"/> (replacing what is queued only if
    /// <paramref name="replaceQueue"/>), marks the queue as held and returns true.
    /// </summary>
    public async Task<bool> ParkIfHeld(ModuleSaga saga, DesiredStateHeadline? stateToQueue, bool replaceQueue)
    {
        var state = await _holds.GetHoldState(saga.OrganizationId, saga.CorrelationId);
        if (!state.Held)
            return false;

        if (stateToQueue != null && (replaceQueue || saga.QueuedDesiredStateHeadline == null))
            saga.QueuedDesiredStateHeadline = stateToQueue;

        if (saga.QueuedDesiredStateHeadline == null)
            return true;

        if (saga.QueuedReason != QueuedReason.HeldBySession)
        {
            saga.QueuedReason = QueuedReason.HeldBySession;
            await ScheduleCheck(saga, state.ExpiresAt!.Value);
        }

        _logger.LogDebug("Module {ModuleId} is held until at least {ExpiresAt}: parking {State}",
            saga.CorrelationId, state.ExpiresAt, saga.QueuedDesiredStateHeadline);
        return true;
    }

    /// <summary>Checks again a second after the hold would expire, so the release lands just after it.</summary>
    public Task ScheduleCheck(ModuleSaga saga, DateTime expiresAt) =>
        Scheduler.SchedulePublish(expiresAt.AddSeconds(1), new ModuleHoldCheck
        {
            ModuleId = saga.CorrelationId,
            OrganizationId = saga.OrganizationId
        });
}
