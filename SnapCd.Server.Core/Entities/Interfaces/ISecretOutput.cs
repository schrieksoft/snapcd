using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface ISecretOutput : ISecretScoped
{
    public Guid OutputSetId { get; set; }
    public OutputSet OutputSet { get; set; }
}