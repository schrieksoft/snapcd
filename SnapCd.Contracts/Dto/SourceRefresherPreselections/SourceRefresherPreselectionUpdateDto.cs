using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.SourceRefresherPreselections;

public class SourceRefresherPreselectionUpdateDto : SourceRefresherPreselectionCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
