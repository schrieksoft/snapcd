// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class ModuleModifiedCompetingConsumer : IConsumer<ModuleModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;
    private readonly ILogger<ModuleModifiedCompetingConsumer> _logger;

    public ModuleModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus, ILogger<ModuleModifiedCompetingConsumer> logger)
    {
        _dbContext = dbContext;
        _bus = bus;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ModuleModifiedEvent> context)
    {
        var triggerOnDefinitionChanged = _dbContext
            .Modules.Where(x => x.Id == context.Message.Id)
            .Select(x => x.TriggerOnDefinitionChanged)
            .FirstOrDefault();

        if (triggerOnDefinitionChanged)
        {
            _logger.LogDebug("Publishing ModuleModifiedTriggerRequested for module {ModuleId}", context.Message.Id);
            await _bus.Publish(new ModuleModifiedTriggerRequested { ModuleId = context.Message.Id, OrganizationId = context.Message.OrganizationId });
        }
    }
}