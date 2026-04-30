namespace SnapCd.Server.Core.Events.Steps.Base;

/// <summary>
/// Base class for all step faulted events.
/// Contains common error information and the IsServerSideError flag to distinguish
/// between server-side errors (in consumers/activities) and runner-side errors.
/// </summary>
public abstract class StepFaultedBase : StepResponseBase
{
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }

    /// <summary>
    /// When true, indicates the error occurred on the server side (in consumers or activities).
    /// When false (default), indicates the error occurred on the runner side.
    /// </summary>
    public bool IsServerSideError { get; set; }
}
