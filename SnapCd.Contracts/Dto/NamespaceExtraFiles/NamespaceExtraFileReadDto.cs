using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceExtraFiles;

public class NamespaceExtraFileReadDto : NamespaceExtraFileCreateDto, IDto
{
    public Guid Id { get; set; }
}