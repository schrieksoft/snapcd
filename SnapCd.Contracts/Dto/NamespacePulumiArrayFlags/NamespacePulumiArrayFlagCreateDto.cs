using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;

public class NamespacePulumiArrayFlagCreateDto
{
    public PulumiCommandTask Task { get; set; }

    public PulumiArrayFlag Flag { get; set; }

    [Required] [MaxLength(1000)] public string Value { get; set; } = null!;

    public Guid NamespaceId { get; set; }
}
