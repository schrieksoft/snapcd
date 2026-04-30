using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ServicePrincipals;

/// <summary>
/// DTO for ServicePrincipal responses (GET operations).
/// </summary>
public class ServicePrincipalReadDto : ServicePrincipalCreateDto, IDto
{
    public Guid Id { get; set; }
}
