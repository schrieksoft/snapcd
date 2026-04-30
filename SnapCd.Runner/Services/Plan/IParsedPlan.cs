using SnapCd.Contracts;

namespace SnapCd.Runner.Services.Plan;

public interface IParsedPlan
{
    int GetExistingCount();
    int GetResourceCount(PlanAction action);
    int GetOutputCount(PlanAction action);
    List<PlanResourceChange> GetResourceChange(PlanAction action);
    List<PlanOutputChange> GetOutputChange(PlanAction action);
}

public class PlanResourceChange
{
    public string Address { get; set; } = "";
    public PlanAction Action { get; set; }
}

public class PlanOutputChange
{
    public string Name { get; set; } = "";
    public PlanAction Action { get; set; }
}
