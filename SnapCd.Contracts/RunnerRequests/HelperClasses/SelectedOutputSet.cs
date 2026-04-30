namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

public class SelectedOutputSet
{
    public required string Name { get; set; }

    public Guid ModuleId { get; set; }

    public Guid? OutputSetId { get; set; }
}