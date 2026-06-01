// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

public class ModuleJobApprovalModifiedFanoutConsumer :
    IConsumer<ModuleJobApprovalCreatedEvent>,
    IConsumer<ModuleJobApprovalUpdatedEvent>,
    IConsumer<ModuleJobApprovalDeletedEvent>
{
    private readonly ModuleJobApprovalModifiedNotificationService _notificationService;
    private readonly SnapCdDbContext _dbContext;

    public ModuleJobApprovalModifiedFanoutConsumer(
        ModuleJobApprovalModifiedNotificationService notificationService,
        SnapCdDbContext dbContext)
    {
        _notificationService = notificationService;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<ModuleJobApprovalCreatedEvent> context)
    {
        await NotifyForModuleJob(context.Message.Data.ModuleJobId);
    }

    public async Task Consume(ConsumeContext<ModuleJobApprovalUpdatedEvent> context)
    {
        await NotifyForModuleJob(context.Message.Data.ModuleJobId);
    }

    public async Task Consume(ConsumeContext<ModuleJobApprovalDeletedEvent> context)
    {
        await NotifyForModuleJob(context.Message.Data.ModuleJobId);
    }

    private async Task NotifyForModuleJob(Guid moduleJobId)
    {
        var moduleId = await _dbContext.ModuleJobs
            .Where(j => j.Id == moduleJobId)
            .Select(j => j.ModuleId)
            .FirstOrDefaultAsync();

        if (moduleId == default)
            return;

        await _notificationService.Notify(moduleJobId, moduleId);
    }
}
