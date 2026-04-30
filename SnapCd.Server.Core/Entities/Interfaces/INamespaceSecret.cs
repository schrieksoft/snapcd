using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface INamespaceSecret : ISecretScoped
{
    public Guid NamespaceId { get; set; }

    public Namespace Namespace { get; set; }
}