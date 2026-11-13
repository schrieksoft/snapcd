// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class NamespaceApprovalThresholdModifiedCompetingConsumer : IConsumer<NamespaceApprovalThresholdModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public NamespaceApprovalThresholdModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<NamespaceApprovalThresholdModifiedEvent> context)
    {
        // Every Module below this Namespace inherits the threshold, not only those directly in it,
        // so the fan-out follows the descendant side of the closure.
        var descendantIds = _dbContext.NamespaceAncestors
            .Where(a => a.AncestorNamespaceId == context.Message.NamespaceId)
            .Select(a => a.NamespaceId);

        var moduleIds = _dbContext.Modules
            .Where(x => descendantIds.Contains(x.NamespaceId))
            .Select(x => x.Id).ToList();

        foreach (var moduleId in moduleIds) await _bus.Publish(new ModuleApprovalThresholdModifiedEvent { ModuleId = moduleId });
    }
}