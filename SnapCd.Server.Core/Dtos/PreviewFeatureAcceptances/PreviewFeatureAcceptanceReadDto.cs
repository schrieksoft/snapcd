using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.PreviewFeatureAcceptances;

public class PreviewFeatureAcceptanceReadDto : PreviewFeatureAcceptanceCreateDto, IDto
{
    public Guid Id { get; set; }
}
