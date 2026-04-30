namespace SnapCd.Server.Core.Dtos;

public class ModuleDirectoryInfoDto
{
    public string StackName { get; set; } = null!;
    public string NamespaceName { get; set; } = null!;
    public string ModuleName { get; set; } = null!;
    public string SourceSubdirectory { get; set; } = null!;
    public string WorkingDirectory { get; set; } = null!;
    public string ModuleRootDir { get; set; } = null!;
    public string InitDir { get; set; } = null!;
}