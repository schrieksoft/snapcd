using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleInputFromOutputSet : ModuleInputWithOutputModuleId, IModuleInputFromOutputSet
{
    [JsonIgnore] public Module OutputModule { get; set; } = null!;
}

public class ModuleParamFromOutputSet : ModuleInputFromOutputSet
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}