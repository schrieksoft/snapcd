using SnapCd.Contracts.Dto.NamespaceInputs.Base;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromDefinitionCreateDto : NamespaceInputCreateDto
{
    public DefinitionInputType DefinitionName { get; set; }
}
