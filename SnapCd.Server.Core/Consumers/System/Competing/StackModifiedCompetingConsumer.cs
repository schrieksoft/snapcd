// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class StackModifiedCompetingConsumer : IConsumer<StackUpdatedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public StackModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<StackUpdatedEvent> context)
    {
        var triggerBehaviour = _dbContext
            .Stacks.Where(x => x.Id == context.Message.Data.Id)
            .Select(x => (StackTriggerBehaviour?)x.TriggerBehaviourOnModified)
            .FirstOrDefault();

        if (triggerBehaviour == StackTriggerBehaviour.TriggerAllImmediately)
        {
            var moduleIds = _dbContext.Modules
                .Include(x => x.Namespace)
                .Where(x => x.Namespace.StackId == context.Message.Data.Id && x.TriggerOnDefinitionChanged)
                .Select(x => x.Namespace.StackId).ToList();

            foreach (var moduleId in moduleIds) await _bus.Publish(new ModuleModifiedTriggerRequested { ModuleId = moduleId, OrganizationId = context.Message.Data.Id });
        }
    }
}