using SnapCd.Server.Core.Entities.Definition.Secrets;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IModuleInputFromSecret
{
    Guid SecretId { get; set; }
    Secret Secret { get; set; }
}