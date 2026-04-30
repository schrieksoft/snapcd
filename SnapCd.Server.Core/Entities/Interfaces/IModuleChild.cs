using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IModuleChild
{
    public Guid ModuleId { get; set; }

    public Module Module { get; set; }
}