using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IStackChild
{
    public Guid StackId { get; set; }

    public Stack Stack { get; set; }
}