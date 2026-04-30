using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface INamespaceInputFromLiteral
{
    public string LiteralValue { get; set; }

    public InputType Type { get; set; }
}