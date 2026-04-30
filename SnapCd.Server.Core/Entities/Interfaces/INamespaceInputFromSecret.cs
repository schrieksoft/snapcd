using SnapCd.Server.Core.Entities.Definition.Secrets;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface INamespaceInputFromSecret
{
    Guid SecretId { get; set; }
    Secret Secret { get; set; }
    string SecretName { get; }
}