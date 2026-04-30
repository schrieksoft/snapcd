namespace SnapCd.Contracts.Dto.SourceRefresherPreselections;

public class SourceRefresherPreselectionCreateDto
{
    public Guid RunnerId { get; set; }

    public string? RunnerInstanceName { get; set; }

    public string SourceUrl { get; set; } = null!;
}
