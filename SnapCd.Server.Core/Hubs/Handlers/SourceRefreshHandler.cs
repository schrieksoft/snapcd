// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion and fault notifications from runners for source refresh operations.
/// This is a stateless handler - no request tracking needed since responses are matched by source parameters.
/// </summary>
public class SourceRefreshHandler
{
    private readonly ILogger<SourceRefreshHandler> _logger;
    private readonly IBus _bus;

    public SourceRefreshHandler(
        ILogger<SourceRefreshHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(string sourceUrl, string sourceRevision, SourceType sourceType, SourceRevisionType sourceRevisionType, string definitiveRevision)
    {
        try
        {
            _logger.LogInformation("Runner completed source refresh for {SourceUrl} @ {SourceRevision}", sourceUrl, sourceRevision);

            // Publish event directly - no tracking needed
            // SourceRefreshCompletedCompetingConsumer will match by source parameters
            await _bus.Publish(new SourceRefreshCompleted
            {
                SourceUrl = sourceUrl,
                SourceRevision = sourceRevision,
                SourceType = sourceType,
                SourceRevisionType = sourceRevisionType,
                DefinitiveRevision = definitiveRevision
            });

            _logger.LogInformation("Published SourceRefreshCompleted for {SourceUrl} @ {SourceRevision} -> {DefinitiveRevision}",
                sourceUrl, sourceRevision, definitiveRevision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing source refresh completion for {SourceUrl} @ {SourceRevision}",
                sourceUrl, sourceRevision);
            throw;
        }
    }

    public async Task CompleteV2(string sourceUrl, string sourceRevision, SourceType sourceType, SourceRevisionType sourceRevisionType, SourceRefreshResult result)
    {
        try
        {
            _logger.LogInformation("Runner completed path-aware source refresh for {SourceUrl} @ {SourceRevision} ({PathCount} paths)",
                sourceUrl, sourceRevision, result.PathHashes.Count);

            await _bus.Publish(new SourceRefreshCompleted
            {
                SourceUrl = sourceUrl,
                SourceRevision = sourceRevision,
                SourceType = sourceType,
                SourceRevisionType = sourceRevisionType,
                DefinitiveRevision = result.DefinitiveRevision,
                PathHashes = result.PathHashes,
                ModuleClosures = result.ModuleClosures,
                TriggeredByNotification = result.TriggeredByNotification
            });

            _logger.LogInformation("Published SourceRefreshCompleted for {SourceUrl} @ {SourceRevision} -> {DefinitiveRevision} with {PathCount} path hashes",
                sourceUrl, sourceRevision, result.DefinitiveRevision, result.PathHashes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing path-aware source refresh completion for {SourceUrl} @ {SourceRevision}",
                sourceUrl, sourceRevision);
            throw;
        }
    }

    public async Task Fault(string sourceUrl, string sourceRevision, SourceType sourceType, SourceRevisionType sourceRevisionType, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted source refresh for {SourceUrl} @ {SourceRevision}: {ErrorMessage}",
                sourceUrl, sourceRevision, errorMessage);

            // Publish fault event directly - no tracking needed
            await _bus.Publish(new SourceRefreshFaulted
            {
                SourceUrl = sourceUrl,
                SourceRevision = sourceRevision,
                SourceType = sourceType,
                SourceRevisionType = sourceRevisionType,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace
            });

            _logger.LogInformation("Published SourceRefreshFaulted for {SourceUrl} @ {SourceRevision}",
                sourceUrl, sourceRevision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing source refresh fault for {SourceUrl} @ {SourceRevision}",
                sourceUrl, sourceRevision);
            throw;
        }
    }
}
