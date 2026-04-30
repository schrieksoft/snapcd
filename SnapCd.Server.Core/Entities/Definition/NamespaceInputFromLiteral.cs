using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class NamespaceInputFromLiteral : NamespaceInputWithType, INamespaceInputFromLiteral
{
    [MaxLength(4000)] public string LiteralValue { get; set; } = null!;
}

public class NamespaceParamFromLiteral : NamespaceInputFromLiteral
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}

public class NamespaceEnvVarFromLiteral : NamespaceInputFromLiteral
{
    public override InputKind InputKind { get; init; } = InputKind.EnvVar;
}