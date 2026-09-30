// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class StateMigrationUpdatedFanoutConsumer : IConsumer<StateMigrationUpdatedEvent>
{
    private readonly StateMigrationUpdatedNotificationService _notificationService;
    private readonly ILogger<StateMigrationUpdatedFanoutConsumer> _logger;

    public StateMigrationUpdatedFanoutConsumer(
        StateMigrationUpdatedNotificationService notificationService,
        ILogger<StateMigrationUpdatedFanoutConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StateMigrationUpdatedEvent> context)
    {
        _logger.LogDebug(
            "Telling open pages that manual job {JobId} on Module {ModuleId} changed",
            context.Message.JobId, context.Message.ModuleId);

        await _notificationService.Notify(context.Message.JobId, context.Message.ModuleId);
    }
}
