using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Definition.Base;

public class NamespaceInputWithType : NamespaceInput
{
    public InputType Type { get; set; }
}