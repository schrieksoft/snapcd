using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IModuleInputFromOutputSet
{
    public Guid Id { get; set; }

    public string Name { get; set; }
    public Guid OutputModuleId { get; set; }
    public Module OutputModule { get; set; }
}