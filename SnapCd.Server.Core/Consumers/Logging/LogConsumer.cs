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