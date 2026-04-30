using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class NamespaceInputFromDefinition : NamespaceInput, INamespaceInputFromDefinition
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DefinitionInputType DefinitionName { get; set; }
}

public class NamespaceParamFromDefinition : NamespaceInputFromDefinition
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}

public class NamespaceEnvVarFromDefinition : NamespaceInputFromDefinition
{
    public override InputKind InputKind { get; init; } = InputKind.EnvVar;
}