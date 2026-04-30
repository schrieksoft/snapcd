namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

public class SelectedNamespaceSecret
{
    public required string InputName { get; set; }
    
    public required Guid InputId { get; set; }

    public required NamespaceInputUsageMode UsageMode { get; set; }
    public required SecretDiscriminator Discriminator { get; set; }

    public required InputType Type { get; set; }

    public required string NamespaceName { get; set; }

    public required Guid NamespaceId { get; set; }

    public Guid? SecretId { get; set; }

    public string? SecretName { get; set; }

    public required string Hash { get; set; }
}