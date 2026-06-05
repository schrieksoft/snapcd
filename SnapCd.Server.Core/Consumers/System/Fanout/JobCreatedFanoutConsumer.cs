// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class JobCreatedFanoutConsumer : IConsumer<ModuleJobCreatedEvent>
{
    private readonly JobCreatedNotificationService _notificationService;

    public JobCreatedFanoutConsumer(JobCreatedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<ModuleJobCreatedEvent> context)
    {
        await _notificationService.Notify(
            context.Message.Data.Id,
            context.Message.Data.ModuleId
        );
    }
}
