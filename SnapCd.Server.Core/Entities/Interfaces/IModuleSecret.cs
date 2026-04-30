using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IModuleSecret : ISecretScoped
{
    public Guid ModuleId { get; set; }

    public Module Module { get; set; }
}