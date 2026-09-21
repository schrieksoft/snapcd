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
/// Turns a withdrawn Module's hold into a pause in one step, so there is no window in which the
/// gate is open on a Module whose state has not caught up with its code.
/// </summary>
public class ModuleWithdrawnFromTransferCompetingConsumer : IConsumer<ModuleWithdrawnFromTransfer>
{
    private readonly ModuleSagaRepositoryFactory _sagaRepositoryFactory;
    private readonly ILogger<ModuleWithdrawnFromTransferCompetingConsumer> _logger;

    public ModuleWithdrawnFromTransferCompetingConsumer(
        ModuleSagaRepositoryFactory sagaRepositoryFactory,
        ILogger<ModuleWithdrawnFromTransferCompetingConsumer> logger)
    {
        _sagaRepositoryFactory = sagaRepositoryFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ModuleWithdrawnFromTransfer> context)
    {
        using var repository = _sagaRepositoryFactory.Create();

        await repository.ConvertHoldToPause(
            context.Message.ModuleId,
            context.Message.OrganizationId,
            context.Message.TransferId,
            context.Message.Reason);

        _logger.LogInformation(
            "Module {ModuleId} withdrawn from transfer {TransferId}: its hold is now a pause",
            context.Message.ModuleId, context.Message.TransferId);
    }
}
