using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IModuleInputFromLiteral
{
    public string LiteralValue { get; set; }

    public InputType Type { get; set; }
}