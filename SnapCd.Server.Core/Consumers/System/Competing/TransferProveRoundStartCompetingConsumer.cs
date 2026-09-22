// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Transfers;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Turns a request for a prove round into the right instruction: the coordinator has to exist
/// before it can be told to run a round, and it is created once per Transfer rather than once per
/// round. Callers ask for a round and this decides which.
/// </summary>
public class TransferProveRoundStartCompetingConsumer : IConsumer<TransferProveRoundStartRequested>
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IBus _bus;
    private readonly ILogger<TransferProveRoundStartCompetingConsumer> _logger;

    public TransferProveRoundStartCompetingConsumer(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IBus bus,
        ILogger<TransferProveRoundStartCompetingConsumer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _bus = bus;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransferProveRoundStartRequested> context)
    {
        var message = context.Message;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var coordinator = await dbContext.Set<TransferSaga>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.TransferId == message.TransferId
                                      && s.OrganizationId == message.OrganizationId);

        if (coordinator == null)
        {
            _logger.LogInformation(
                "Transfer {TransferId}: opening, and starting its first prove round",
                message.TransferId);

            await _bus.Publish(new TransferOpened
            {
                CorrelationId = Guid.NewGuid(),
                OrganizationId = message.OrganizationId,
                TransferId = message.TransferId,
                SourceModuleId = message.SourceModuleId,
                ReceiverModuleId = message.ReceiverModuleId,
                MapHash = message.MapHash,
                SourceDeclared = message.SourceDeclared,
                ReceiverDeclared = message.ReceiverDeclared,
                SourceRootDirectory = message.SourceRootDirectory,
                ReceiverRootDirectory = message.ReceiverRootDirectory
            });

            coordinator = await WaitForCoordinator(message);
        }

        // Both Module sagas have to exist before a round is asked for: a round arriving earlier
        // has nothing to dispatch to.
        await WaitForModuleSagas(message);

        var round = Round(message);
        round.CorrelationId = coordinator!.CorrelationId;
        await _bus.Publish(round);
    }

    private async Task<TransferSaga?> WaitForCoordinator(TransferProveRoundStartRequested message)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var saga = await dbContext.Set<TransferSaga>().AsNoTracking()
                .FirstOrDefaultAsync(s => s.TransferId == message.TransferId
                                          && s.OrganizationId == message.OrganizationId);

            if (saga != null) return saga;
            await Task.Delay(100);
        }

        throw new InvalidOperationException(
            $"Transfer '{message.TransferId}' did not open in time to start a round.");
    }

    private async Task WaitForModuleSagas(TransferProveRoundStartRequested message)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var count = await dbContext.Set<TransferParticipantSaga>().AsNoTracking()
                .CountAsync(s => s.TransferId == message.TransferId
                                 && s.OrganizationId == message.OrganizationId);

            if (count == 2) return;
            await Task.Delay(100);
        }

        throw new InvalidOperationException(
            $"Transfer '{message.TransferId}' has no Module sagas to run a round against.");
    }

    private static TransferProveRoundRequested Round(TransferProveRoundStartRequested message) =>
        new()
        {
            OrganizationId = message.OrganizationId,
            JobId = message.JobId,
            Map = message.Map,
            SourceProveRef = message.SourceProveRef,
            ReceiverProveRef = message.ReceiverProveRef
        };
}
