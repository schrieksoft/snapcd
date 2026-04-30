using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

public class NamespaceSecret : Secret, INamespaceSecret, INamespaceChild
{
    public override SecretScope ScopeKind { get; init; } = SecretScope.Namespace;

    public Guid NamespaceId { get; set; }
    public Namespace Namespace { get; set; } = null!;

    public override Guid ParentId()
    {
        return NamespaceId;
    }

    public virtual SecretDiscriminator GetSecretDiscriminator()
    {
        return SecretDiscriminator.NamespaceSecret;
    }
}