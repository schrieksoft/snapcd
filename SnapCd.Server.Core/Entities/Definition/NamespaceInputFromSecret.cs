using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class NamespaceInputFromSecret : NamespaceInputWithType, INamespaceInputFromSecret
{
    public Guid SecretId { get; set; }
    public Secret Secret { get; set; } = null!;

    public string SecretName => Secret?.Name ?? string.Empty;
}

public class NamespaceParamFromSecret : NamespaceInputFromSecret
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}

public class NamespaceEnvVarFromSecret : NamespaceInputFromSecret
{
    public override InputKind InputKind { get; init; } = InputKind.EnvVar;
}