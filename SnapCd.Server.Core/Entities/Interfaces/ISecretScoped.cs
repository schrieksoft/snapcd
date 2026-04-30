using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface ISecretScoped : IEntity
{
    public string Name { get; set; }

    public SecretDiscriminator GetSecretDiscriminator();
}