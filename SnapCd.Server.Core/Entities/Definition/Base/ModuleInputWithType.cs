using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Definition.Base;

public class ModuleInputWithType : ModuleInput
{
    public InputType Type { get; set; }
}