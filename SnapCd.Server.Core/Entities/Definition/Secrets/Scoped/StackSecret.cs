using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

public class StackSecret : Secret, IStackSecret, IStackChild
{
    public override SecretScope ScopeKind { get; init; } = SecretScope.Stack;
    public Guid StackId { get; set; }

    public Stack Stack { get; set; } = null!;

    public override Guid ParentId()
    {
        return StackId;
    }

    public virtual SecretDiscriminator GetSecretDiscriminator()
    {
        return SecretDiscriminator.StackSecret;
    }
}