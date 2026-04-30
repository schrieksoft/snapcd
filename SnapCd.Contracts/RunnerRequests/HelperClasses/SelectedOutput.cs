namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

public class SelectedOutput
{
    public required string Name { get; set; }
    public string? OutputName { get; set; }

    public Guid ModuleId { get; set; }


    public Guid? OutputId { get; set; }

    public Guid? OutputSetId { get; set; }

    public required string Hash { get; set; }
}