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
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class SourceRefreshCompletedCompetingConsumer : IConsumer<SourceRefreshCompleted>
{
    private readonly SnapCdDbContext _dbContext;

    public SourceRefreshCompletedCompetingConsumer(
        SnapCdDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<SourceRefreshCompleted> context)
    {
        var message = context.Message;

        var modules = _dbContext.Modules
            .Include(x => x.Runner)
            .Include(x => x.ModuleSaga)
            .Include(x => x.AdditionalTriggerPaths)
            .Include(x => x.Namespace).ThenInclude(n => n.AdditionalTriggerPaths)
            .Where(x => x.SourceUrl == message.SourceUrl &&
                        x.SourceRevision == message.SourceRevision &&
                        x.SourceType == message.SourceType &&
                        (x.TriggerOnSourceChanged || (message.TriggeredByNotification && x.TriggerOnSourceChangedNotification))
            )
            .ToList()
            // Notification-only modules are evaluated exclusively for notification-dispatched refreshes, and only
            // when path-scoped: filter-off notification modules were already triggered directly by
            // SourceChangedService, and the polling schedule must never trigger a notification-only module.
            .Where(x => x.TriggerOnSourceChanged || TriggerPathClosure.FilterEnabled(x))
            .ToList();

        var reportedTreeHashes = message.PathHashes?.ToDictionary(p => p.Path, p => p.TreeHash, StringComparer.Ordinal);
        var closuresByRoot = message.ModuleClosures?.ToDictionary(c => c.RootPath, c => c.ReferencedPaths, StringComparer.Ordinal);

        foreach (var module in modules)
        {
            string? desiredClosureHash = null;
            bool shouldTrigger;

            if (reportedTreeHashes != null && TriggerPathClosure.FilterEnabled(module))
            {
                // Path-scoped decision: trigger iff the composed closure hash moved. Fail-open falls out
                // naturally — a null stored hash never equals a composition, and a watched path missing from
                // the report composes with an empty hash.
                var watchedPaths = TriggerPathClosure.ExpandWithClosures(TriggerPathClosure.WatchedPaths(module), closuresByRoot);
                desiredClosureHash = TriggerPathClosure.Compose(watchedPaths, reportedTreeHashes);
                shouldTrigger = module.ModuleSaga == null || module.ModuleSaga.DesiredClosureHash != desiredClosureHash;
            }
            else
            {
                shouldTrigger = module.ModuleSaga == null || module.ModuleSaga.DesiredDefinitiveRevision != message.DefinitiveRevision;
            }

            if (!shouldTrigger) continue;

            await context.Publish(new GatekeepingJobRequested
            {
                ModuleId = module.Id,
                OrganizationId = module.OrganizationId,
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                SetNewDesiredState = false,
                DefinitiveRevision = message.DefinitiveRevision,
                DesiredClosureHash = desiredClosureHash
            }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); });
        }
    }
}