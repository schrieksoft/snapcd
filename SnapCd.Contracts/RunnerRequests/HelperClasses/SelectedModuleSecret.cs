namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

public class SelectedModuleSecret
{
    public required string InputName { get; set; }

    public required SecretDiscriminator Discriminator { get; set; }

    public required InputType Type { get; set; }

    public required string ModuleName { get; set; }

    public required Guid ModuleId { get; set; }

    public Guid? SecretId { get; set; }

    public string? SecretName { get; set; }

    public required string Hash { get; set; }
}