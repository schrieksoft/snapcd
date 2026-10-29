// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Utils.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class ProbeWaveUpdatedFanoutConsumer : IConsumer<ProbeWaveUpdatedEvent>
{
    private readonly ProbeWaveUpdatedNotificationService _notificationService;
    private readonly ILogger<ProbeWaveUpdatedFanoutConsumer> _logger;

    public ProbeWaveUpdatedFanoutConsumer(
        ProbeWaveUpdatedNotificationService notificationService,
        ILogger<ProbeWaveUpdatedFanoutConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProbeWaveUpdatedEvent> context)
    {
        _logger.LogDebug(
            "Telling open pages that wave {ProbeWaveId} changed",
            context.Message.ProbeWaveId);

        await _notificationService.Notify(context.Message.ProbeWaveId, context.Message.ModuleId);
    }
}
