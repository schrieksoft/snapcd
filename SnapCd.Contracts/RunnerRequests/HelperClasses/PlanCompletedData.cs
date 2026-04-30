namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Data returned when a Plan operation completes successfully.
/// Contains all resource and output change statistics.
/// </summary>
public class PlanCompletedData
{
    // Resource counts
    public int TotalCountAfter { get; set; }
    public int TotalCountBefore { get; set; }
    public int TotalChangedCount { get; set; }
    public int TotalUnchangedCount { get; set; }
    public int CreateCount { get; set; }
    public int ModifyCount { get; set; }
    public int DestroyCount { get; set; }
    public int RecreateCount { get; set; }

    // Output counts
    public int OutputsTotalCount { get; set; }
    public int OutputsTotalChangedCount { get; set; }
    public int OutputsTotalUnchangedCount { get; set; }
    public int OutputsCreateCount { get; set; }
    public int OutputsModifyCount { get; set; }
    public int OutputsDestroyCount { get; set; }
    public int OutputsRecreateCount { get; set; }

    // Output name lists
    public List<string>? OutputsUnchangedList { get; set; }
    public List<string>? OutputsCreateList { get; set; }
    public List<string>? OutputsModifyList { get; set; }
    public List<string>? OutputsDestroyList { get; set; }
    public List<string>? OutputsRecreateList { get; set; }
}
