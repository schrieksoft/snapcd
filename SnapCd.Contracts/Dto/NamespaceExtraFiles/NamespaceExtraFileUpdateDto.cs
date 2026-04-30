using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceExtraFiles;

public class NamespaceExtraFileUpdateDto : NamespaceExtraFileCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
