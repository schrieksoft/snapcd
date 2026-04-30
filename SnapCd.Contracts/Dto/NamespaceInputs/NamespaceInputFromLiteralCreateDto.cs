using SnapCd.Contracts.Dto.NamespaceInputs.Base;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromLiteralCreateDto : NamespaceInputCreateDto
{
    public string LiteralValue { get; set; } = null!;

    public InputType Type { get; set; }
}
