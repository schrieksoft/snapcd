namespace SnapCd.Contracts.Dto.Misc;

public class ExtraFileDto
{
    public string FileName { get; set; } = null!;

    public string Contents { get; set; } = null!;

    public bool Overwrite { get; set; }

    public string Source { get; set; } = null!;
}

public enum ExtraFileSource
{
    Namespace,
    Module
}