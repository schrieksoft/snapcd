// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Events.Repository.Organization;

namespace SnapCd.Server.Host.CapAlerts;

/// <summary>Fan-out: every server instance re-evaluates cap alerts for its own open circuits.</summary>
public class ModuleCountChangedFanoutConsumer(ModuleCountChangedNotificationService notifications) :
    IConsumer<ModuleCreatedEvent>,
    IConsumer<ModuleDeletedEvent>
{
    public Task Consume(ConsumeContext<ModuleCreatedEvent> context) => notifications.NotifyAll();

    public Task Consume(ConsumeContext<ModuleDeletedEvent> context) => notifications.NotifyAll();
}
