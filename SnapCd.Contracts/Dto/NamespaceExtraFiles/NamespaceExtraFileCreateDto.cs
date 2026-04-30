namespace SnapCd.Contracts.Dto.NamespaceExtraFiles;

public class NamespaceExtraFileCreateDto
{
    public Guid NamespaceId { get; set; }

    public string FileName { get; set; } = null!;

    public string Contents { get; set; } = null!;

    public bool Overwrite { get; set; }
}
