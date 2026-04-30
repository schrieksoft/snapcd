using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.PreviewFeatureAcceptances;

public class PreviewFeatureAcceptanceUpdateDto : PreviewFeatureAcceptanceCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
