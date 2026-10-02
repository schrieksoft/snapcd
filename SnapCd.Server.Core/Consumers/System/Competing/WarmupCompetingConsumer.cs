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

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Answers a warm-up by logging that it arrived and nothing else. Its own endpoint, so warming the
/// transport touches no queue a real message uses and nothing has to recognise and discard it.
/// </summary>
public class WarmupCompetingConsumer(ILogger<WarmupCompetingConsumer> logger)
    : IConsumer<WarmupRequested>
{
    public Task Consume(ConsumeContext<WarmupRequested> context)
    {
        logger.LogDebug("Warm-up {Sequence} arrived", context.Message.Sequence);
        return Task.CompletedTask;
    }
}
