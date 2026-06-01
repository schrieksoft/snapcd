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

public class ModuleApprovalThresholdModifiedCompetingConsumer : IConsumer<ModuleApprovalThresholdModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public ModuleApprovalThresholdModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<ModuleApprovalThresholdModifiedEvent> context)
    {
        var jobsId = _dbContext.ModuleJobs
            .Where(x => x.ModuleId == context.Message.ModuleId && x.WaitingForApproval == true)
            .Select(x => x.Id).ToList();

        foreach (var jobId in jobsId) await _bus.Publish(new ApprovalReevaluationRequestedEvent { ModuleId = context.Message.ModuleId, ModuleJobId = jobId });
    }
}