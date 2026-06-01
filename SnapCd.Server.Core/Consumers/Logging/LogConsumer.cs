// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Logging;

public class LogConsumer : IConsumer<LogEntryDto>
{
    private readonly LogService _logService;

    public LogConsumer(LogService logService)
    {
        _logService = logService;
    }

    public async Task Consume(ConsumeContext<LogEntryDto> context)
    {
        await _logService.AddLogEntries(new List<LogEntryDto> { context.Message });
    }
}