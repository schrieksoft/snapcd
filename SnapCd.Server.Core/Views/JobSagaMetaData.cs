namespace SnapCd.Server.Core.Views;

/// <summary>
/// Contains essential metadata from a job saga for authorization and validation purposes.
/// </summary>
public class JobSagaMetaData
{
    public required string CurrentState { get; init; }
    public required Guid RunnerId { get; init; }
    public string? RunnerInstanceName { get; init; }
    public required Guid OrganizationId { get; init; }
    public string? PreviousStateBeforeCancelling { get; init; }
}