using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleInputFromDefinition : ModuleInput, IModuleInputFromDefinition
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DefinitionInputType DefinitionName { get; set; }
}

public class ModuleParamFromDefinition : ModuleInputFromDefinition
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}

public class ModuleEnvVarFromDefinition : ModuleInputFromDefinition
{
    public override InputKind InputKind { get; init; } = InputKind.EnvVar;
}