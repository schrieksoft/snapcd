using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

public class ModuleSecret : Secret, IModuleSecret, IModuleChild
{
    public override SecretScope ScopeKind { get; init; } = SecretScope.Module;

    public Guid ModuleId { get; set; }

    public Module Module { get; set; } = null!;

    public override Guid ParentId()
    {
        return ModuleId;
    }

    public virtual SecretDiscriminator GetSecretDiscriminator()
    {
        return SecretDiscriminator.ModuleSecret;
    }
}