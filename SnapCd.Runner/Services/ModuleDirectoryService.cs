using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services;

public class ModuleDirectoryService
{
    protected readonly string ModuleRootDir;
    protected readonly string InitDir;
    protected readonly string SnapCdDir;
    protected readonly string Subdirectory;
    protected readonly WorkingDirectorySettings WorkingDirectorySettings;

    public ModuleDirectoryService(
        JobMetadata metadata,
        IOptions<WorkingDirectorySettings> workingDirectorySettings
    )
    {
        WorkingDirectorySettings = workingDirectorySettings.Value;
        Subdirectory = metadata.SourceSubdirectory ?? string.Empty;
        var relativeModuleDir = $"{metadata.StackName}/{metadata.NamespaceName}/{metadata.ModuleName}";
        ModuleRootDir = Path.Combine(WorkingDirectorySettings.WorkingDirectory, relativeModuleDir);

        InitDir = string.IsNullOrEmpty(Subdirectory) ? ModuleRootDir : Path.Combine(ModuleRootDir, Subdirectory);
        SnapCdDir = Path.Combine(InitDir, ".snapcd");
    }


    public virtual string GetWorkingDir()
    {
        return WorkingDirectorySettings.WorkingDirectory;
    }

    public virtual string GetTempDir()
    {
        return WorkingDirectorySettings.TempDirectory;
    }

    public virtual string GetModuleRootDir()
    {
        return ModuleRootDir;
    }

    public virtual string GetInitDir()
    {
        return InitDir;
    }

    public virtual string GetSnapCdDir()
    {
        return SnapCdDir;
    }
}