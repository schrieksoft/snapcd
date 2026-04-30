using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.SourceRefresherPreselections;

public class SourceRefresherPreselectionReadDto : SourceRefresherPreselectionCreateDto, IDto
{
    public Guid Id { get; set; }
}