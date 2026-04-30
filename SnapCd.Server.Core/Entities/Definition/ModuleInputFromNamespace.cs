using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleInputFromNamespace : ModuleInput, IModuleInputFromNamespace
{
    public Guid NamespaceInputId { get; set; }
}

public class ModuleParamFromNamespace : ModuleInputFromNamespace
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}

public class ModuleEnvVarFromNamespace : ModuleInputFromNamespace
{
    public override InputKind InputKind { get; init; } = InputKind.EnvVar;
}