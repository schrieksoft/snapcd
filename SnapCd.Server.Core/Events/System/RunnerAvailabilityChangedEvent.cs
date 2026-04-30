namespace SnapCd.Server.Core.Events.System;

public class RunnerAvailabilityChangedEvent
{
    public Guid RunnerId { get; set; }

    public required string RunnerInstanceName { get; set; }
}