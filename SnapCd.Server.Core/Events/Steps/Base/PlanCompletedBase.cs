namespace SnapCd.Server.Core.Events.Steps.Base;

public class PlanCompletedBase : StepResponseBase
{
    public int TotalCountAfter { get; set; }
    public int TotalCountBefore { get; set; }
    public int TotalChangedCount { get; set; }
    public int TotalUnchangedCount { get; set; }
    public int CreateCount { get; set; }
    public int ModifyCount { get; set; }
    public int DestroyCount { get; set; }
    public int RecreateCount { get; set; }

    public int OutputsTotalCount { get; set; }
    public int OutputsTotalChangedCount { get; set; }
    public int OutputsTotalUnchangedCount { get; set; }
    public int OutputsCreateCount { get; set; }
    public int OutputsModifyCount { get; set; }
    public int OutputsDestroyCount { get; set; }
    public int OutputsRecreateCount { get; set; }

    public string? OutputsUnchangedList { get; set; }
    public string? OutputsCreateList { get; set; }
    public string? OutputsModifyList { get; set; }
    public string? OutputsDestroyList { get; set; }
    public string? OutputsRecreateList { get; set; }
}