using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.NamespacePulumiFlags;

public class NamespacePulumiFlagCreateDto
{
    public PulumiCommandTask Task { get; set; }

    public PulumiFlag Flag { get; set; }

    [MaxLength(1000)] public string? Value { get; set; }

    public Guid NamespaceId { get; set; }
}
