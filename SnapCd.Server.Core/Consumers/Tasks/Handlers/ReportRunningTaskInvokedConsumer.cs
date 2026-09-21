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
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.Handlers;

/// <summary>
/// Handles database work for ReportRunningTaskHandler.Report() invocations.
/// This consumer processes ReportRunningTaskInvoked events to track running tasks
/// without blocking the SignalR connection.
/// </summary>
public class ReportRunningTaskInvokedConsumer : IConsumer<ReportRunningTaskInvoked>
{
    private readonly ILogger<ReportRunningTaskInvokedConsumer> _logger;
    private readonly RunnerConnectionJobRepositoryFactory _runnerConnectionJobRepositoryFactory;
    private readonly RunnerConnectionManualJobService _runnerConnectionManualJobService;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public ReportRunningTaskInvokedConsumer(
        ILogger<ReportRunningTaskInvokedConsumer> logger,
        RunnerConnectionJobRepositoryFactory runnerConnectionJobRepositoryFactory,
        RunnerConnectionManualJobService runnerConnectionManualJobService,
        IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _logger = logger;
        _runnerConnectionJobRepositoryFactory = runnerConnectionJobRepositoryFactory;
        _runnerConnectionManualJobService = runnerConnectionManualJobService;
        _dbContextFactory = dbContextFactory;
    }

    public async Task Consume(ConsumeContext<ReportRunningTaskInvoked> context)
    {
        var message = context.Message;

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var isManualJob = await db.ManualModuleJobs
                .AnyAsync(j => j.Id == message.JobId && j.OrganizationId == message.OrganizationId);

            if (isManualJob)
            {
                await _runnerConnectionManualJobService.CreateOrUpdate(
                    message.OrganizationId,
                    message.JobId,
                    message.TaskName,
                    message.RunnerId,
                    message.RunnerInstanceName);
            }
            else
            {
                using var repository = _runnerConnectionJobRepositoryFactory.Create();
                await repository.CreateOrUpdate(
                    message.OrganizationId,
                    message.JobId,
                    message.TaskName,
                    message.RunnerId,
                    message.RunnerInstanceName);
            }

            _logger.LogDebug(
                "Recorded running task {TaskName} for job {JobId} on runner {RunnerId}",
                message.TaskName, message.JobId, message.RunnerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to record running task {TaskName} for job {JobId}",
                message.TaskName, message.JobId);
            // Don't rethrow - this is tracking information, not critical
        }
    }
}
