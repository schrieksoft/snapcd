using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.Outputs;

public class SecretOutput : Output, ISecretOutput
{
    [MaxLength(255)] public string RemoteSecretName { get; set; } = null!;

    public virtual SecretDiscriminator GetSecretDiscriminator()
    {
        return SecretDiscriminator.SecretOutput;
    }
}