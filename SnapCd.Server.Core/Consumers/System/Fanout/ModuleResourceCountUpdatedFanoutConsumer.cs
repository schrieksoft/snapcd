// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class ModuleResourceCountUpdatedFanoutConsumer : IConsumer<ModuleResourceCountUpdatedEvent>
{
    private readonly ILogger<ModuleResourceCountUpdatedFanoutConsumer> _logger;

    public ModuleResourceCountUpdatedFanoutConsumer(
        ILogger<ModuleResourceCountUpdatedFanoutConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ModuleResourceCountUpdatedEvent> context)
    {
        _logger.LogDebug("Module resource count updated event received for ModuleId: {ModuleId}, invalidating usage cache",
            context.Message.ModuleId);
        return Task.CompletedTask;
    }
}