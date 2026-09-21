// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Takes and releases a Transfer's hold on a Module. Holds are server-owned rather than operator
/// actions, so these bypass the secured repository: the authorization happened on the Transfer.
/// </summary>
public class ModuleHoldCompetingConsumer :
    IConsumer<ModuleHoldRequested>,
    IConsumer<ModuleReleaseRequested>
{
    private readonly ModuleSagaRepositoryFactory _sagaRepositoryFactory;
    private readonly ILogger<ModuleHoldCompetingConsumer> _logger;

    public ModuleHoldCompetingConsumer(
        ModuleSagaRepositoryFactory sagaRepositoryFactory,
        ILogger<ModuleHoldCompetingConsumer> logger)
    {
        _sagaRepositoryFactory = sagaRepositoryFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ModuleHoldRequested> context)
    {
        using var repository = _sagaRepositoryFactory.Create();

        var taken = await repository.TakeHold(
            context.Message.ModuleId, context.Message.OrganizationId, context.Message.TransferId);

        if (taken)
            _logger.LogInformation("Module {ModuleId} held by transfer {TransferId}",
                context.Message.ModuleId, context.Message.TransferId);
        else
            _logger.LogWarning("Module {ModuleId} is already held by another transfer; {TransferId} did not take it",
                context.Message.ModuleId, context.Message.TransferId);
    }

    public async Task Consume(ConsumeContext<ModuleReleaseRequested> context)
    {
        using var repository = _sagaRepositoryFactory.Create();

        var released = await repository.ReleaseHold(
            context.Message.ModuleId, context.Message.OrganizationId, context.Message.TransferId);

        if (released)
            _logger.LogInformation("Module {ModuleId} released by transfer {TransferId}",
                context.Message.ModuleId, context.Message.TransferId);
        else
            _logger.LogDebug("Module {ModuleId} was not held by transfer {TransferId}; nothing released",
                context.Message.ModuleId, context.Message.TransferId);
    }
}
