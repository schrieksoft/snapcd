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
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Services.Secured.Crud;

namespace SnapCd.Server.Core.Consumers.Tasks.Handlers;

/// <summary>
/// Handles database work for OutputHandler.Complete() invocations.
/// This consumer processes OutputCompletedInvoked events to store output sets
/// without blocking the SignalR connection.
/// </summary>
public abstract class OutputCompletedInvokedConsumer<TInvoked, TCompleted, TFaulted> : IConsumer<TInvoked>
    where TInvoked : OutputCompletedInvokedBase
    where TCompleted : StepResponseBase, new()
    where TFaulted : StepFaultedBase, new()
{
    private readonly ILogger _logger;
    private readonly SnapCdDbContext _dbContext;
    private readonly OutputSetService _outputSetService;
    private readonly IBus _bus;

    protected OutputCompletedInvokedConsumer(
        ILogger logger,
        SnapCdDbContext dbContext,
        OutputSetService outputSetService, IBus bus)
    {
        _logger = logger;
        _dbContext = dbContext;
        _outputSetService = outputSetService;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<TInvoked> context)
    {
        var jobId = context.Message.JobId;
        var outputSet = context.Message.OutputSet;

        if (outputSet == null)
        {
            _logger.LogDebug("No output set provided for job {JobId}, skipping storage", jobId);
            await _bus.Publish(new TCompleted { CorrelationId = jobId });
            return;
        }

        var organizationId = Guid.Empty;

        try
        {
            var jobView = await _dbContext.ModuleJobs
                .Where(x => x.Id == jobId)
                .Select(x => new { x.OrganizationId, x.ModuleId })
                .FirstOrDefaultAsync();

            if (jobView != null)
            {
                organizationId = jobView.OrganizationId;
                await _outputSetService.CreateWithOutputsNonsecured(outputSet, jobView.ModuleId, jobView.OrganizationId);
                _logger.LogDebug("Stored OutputSet for job {JobId}", jobId);
                
                // Publish saga event
                await _bus.Publish(new TCompleted { CorrelationId = jobId });

                _logger.LogDebug("Output completion processed for job {JobId}", jobId);
            }
            else
            {
                _logger.LogWarning("Could not find module job {JobId} to store OutputSet", jobId);
                await context.Publish(new TFaulted
                {
                    CorrelationId = jobId,
                    ErrorMessage = $"The server could not find job {jobId} to store its outputs.",
                    IsServerSideError = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store OutputSet for job {JobId}", jobId);
            
            await context.Publish(new TFaulted
            {
                CorrelationId = jobId,
                OrganizationId = organizationId,
                ErrorMessage = ex.Message,
                StackTrace = ex.ToString(),
                IsServerSideError = true
            });
        }
    }
}

public class ApplyOutputCompletedInvokedConsumer(
    ILogger<ApplyOutputCompletedInvokedConsumer> logger,
    SnapCdDbContext dbContext,
    OutputSetService outputSetService,
    IBus bus)
    : OutputCompletedInvokedConsumer<ApplyOutputCompletedInvoked, ApplyOutputCompleted, ApplyOutputFaulted>(
        logger, dbContext, outputSetService, bus);

public class DestroyOutputCompletedInvokedConsumer(
    ILogger<DestroyOutputCompletedInvokedConsumer> logger,
    SnapCdDbContext dbContext,
    OutputSetService outputSetService,
    IBus bus)
    : OutputCompletedInvokedConsumer<DestroyOutputCompletedInvoked, DestroyOutputCompleted, DestroyOutputFaulted>(
        logger, dbContext, outputSetService, bus);
