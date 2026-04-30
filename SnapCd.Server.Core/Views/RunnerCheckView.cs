namespace SnapCd.Server.Core.Views;

/// <summary>
/// View model for checking if the current principal can act as a runner for a specific Runner.
/// Used in runner checkin operations to validate permissions.
/// </summary>
public class RunnerCheckView
{
    /// <summary>
    /// The ID of the Runner.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the Runner.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Whether the current principal has the Runner role for this Runner.
    /// </summary>
    public bool CanActAsRunner { get; set; }
}