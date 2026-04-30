using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleInputFromOutput : ModuleInputWithOutputModuleId, IModuleInputFromOutput
{
    [MaxLength(255)] public string OutputName { get; set; } = null!;

    [JsonIgnore] public Module OutputModule { get; set; } = null!;
}

public class ModuleParamFromOutput : ModuleInputFromOutput
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}

public class ModuleEnvVarFromOutput : ModuleInputFromOutput
{
    public override InputKind InputKind { get; init; } = InputKind.EnvVar;
}

public class ModuleInputWithOutputModuleId : ModuleInput
{
    public Guid OutputModuleId { get; set; }
    // Note: OrganizationId inherited from ModuleInput base class is used as part of the composite foreign key
}