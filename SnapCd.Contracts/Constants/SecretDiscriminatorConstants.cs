namespace SnapCd.Contracts.Constants;

public static class SecretDiscriminatorConstants
{
    public static readonly List<SecretDiscriminator> NamespaceSecretDiscriminators =
    [
        SecretDiscriminator.StackSecret,
        SecretDiscriminator.NamespaceSecret,
        SecretDiscriminator.StackSecret
    ];

    public static readonly List<SecretDiscriminator> AllSecretDiscriminators =
    [
        SecretDiscriminator.StackSecret,
        SecretDiscriminator.NamespaceSecret,
        SecretDiscriminator.StackSecret
    ];
}