using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleInputFromLiteral : ModuleInputWithType, IModuleInputFromLiteral
{
    [MaxLength(4000)] public string LiteralValue { get; set; } = null!;
}

public class ModuleParamFromLiteral : ModuleInputFromLiteral
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}

public class ModuleEnvVarFromLiteral : ModuleInputFromLiteral
{
    public override InputKind InputKind { get; init; } = InputKind.EnvVar;
}