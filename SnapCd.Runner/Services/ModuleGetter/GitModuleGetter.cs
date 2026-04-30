using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Services.ModuleGetter;

public class GitModuleGetter : ModuleGetter
{
    private readonly Git _git;

    public GitModuleGetter(
        SourceRevisionType sourceRevisionType,
        string sourceUrl,
        string sourceRevision,
        string? subdirectory,
        ModuleDirectoryService moduleDirectoryService,
        RunnerTaskContext context,
        ILogger<ModuleGetter> logger,
        Git git,
        string engine
    )
        : base(sourceRevisionType, sourceUrl, sourceRevision, subdirectory, moduleDirectoryService, context, logger, engine)
    {
        _git = git;
    }

    protected override Task<bool> ConfirmModuleDownloaded()
    {
        if (!Directory.Exists(ModuleDirectoryService.GetInitDir()))
        {
            var err = $"The relative directory {Subdirectory} does not exist.";
            Context.LogError(err);
            throw new Exception(err);
        }

        try
        {
            var initDir = ModuleDirectoryService.GetInitDir();
            bool hasFiles = Engine == "pulumi"
                ? Directory.EnumerateFiles(initDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Any(file => Path.GetFileName(file).Equals("Pulumi.yaml", StringComparison.OrdinalIgnoreCase) ||
                                 Path.GetFileName(file).Equals("Pulumi.yml", StringComparison.OrdinalIgnoreCase))
                : Directory.EnumerateFiles(initDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Any(file => file.EndsWith(".tf", StringComparison.OrdinalIgnoreCase) ||
                                 file.EndsWith(".tf.json", StringComparison.OrdinalIgnoreCase));

            if (!hasFiles)
            {
                var expected = Engine == "pulumi"
                    ? "Pulumi.yaml or Pulumi.yml"
                    : ".tf or .tf.json";
                var err = $"No {expected} files found in the root directory {Subdirectory}.";
                Context.LogError(err);
                throw new Exception(err);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex) when (!ex.Message.StartsWith("No "))
        {
            var err = $"Error while checking for module files in {Subdirectory}: {ex.Message}";
            Context.LogError(err);
            throw new Exception(err);
        }
    }


    protected override async Task<HashSet<string>> GetWorkingFiles()
    {
        return await _git.GetWorkingFiles(ModuleDirectoryService.GetModuleRootDir());
    }

    protected override Task<string> StashWorkingFiles(HashSet<string> workingFilePaths)
    {
        var tempFolderName = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid().ToString("N")}";
        if (!workingFilePaths.Any()) return Task.FromResult("");

        var tempDir = Path.Combine(TempWorkingFilesStashDir, tempFolderName);
        Directory.CreateDirectory(tempDir);

        foreach (var workingFilePath in workingFilePaths)
            try
            {
                var destPath =
                    Regex.Replace(workingFilePath, $"^{Regex.Escape(ModuleDirectoryService.GetModuleRootDir())}",
                        tempDir); // replace fullRepoPath with tempDir at beginning of path string.
                try
                {
                    var destDir = Path.GetDirectoryName(destPath);
                    if (destDir == null)
                    {
                        var err = $"Unexpected error occured attempting to stash the file \"{workingFilePath}\" to \"{destPath}\". Could not resolve destination directory from path \"{destPath}\"";
                        Context.LogError(err);
                        throw new Exception(err);
                    }

                    Directory.CreateDirectory(destDir);
                    File.Copy(workingFilePath, destPath, false);
                }
                catch (Exception e)
                {
                    var err = $"Unexpected error occured attempting to stash the file \"{workingFilePath}\" to \"{destPath}\". Exception: {e.Message}";
                    Context.LogError(err);
                    throw new Exception(err);
                }
            }
            catch (Exception e)
            {
                var err = $"Unexpected error occured attempting to stash the file \"{workingFilePath}\". Exception: {e.Message}";
                Context.LogError(err);
                throw new Exception(err);
            }

        Context.LogInformation($"Stashed {workingFilePaths.Count} excluded files in temporary storage.");

        return Task.FromResult(tempDir);
    }

    protected override Task UnstashWorkingFiles(HashSet<string> workingFilePaths, string tempDir)
    {
        if (!workingFilePaths.Any()) return Task.CompletedTask;

        foreach (var destPath in workingFilePaths)
            try
            {
                var sourcePath =
                    Regex.Replace(destPath, $"^{Regex.Escape(ModuleDirectoryService.GetModuleRootDir())}",
                        tempDir); // replace fullRepoPath with tempDir at beginning of path string.
                try
                {
                    var destDir = Path.GetDirectoryName(destPath);
                    Directory.CreateDirectory(destDir ?? throw new InvalidOperationException("Could not resolve destination directory"));
                    File.Move(sourcePath, destPath, true);
                }
                catch (Exception e)
                {
                    Context.LogWarning(
                        $"Unexpected error occured attempting to unstash the file \"{sourcePath}\" to \"{destPath}\". Exception: {e.Message}");
                }
            }
            catch (Exception e)
            {
                Context.LogWarning(
                    $"Unexpected error occured attempting to unstash to \"{destPath}\". Exception: {e.Message}");
            }


        Context.LogInformation($"Restored {workingFilePaths.Count} excluded files from temporary storage.");
        return Task.CompletedTask;
    }

    protected override Task<string> GetLocalDefinitiveRevision()
    {
        var sha = _git.GetLatestLocalSha(ModuleDirectoryService.GetModuleRootDir());

        if (string.IsNullOrEmpty(sha))
            Context.LogInformation($"No local sha found");

        Context.LogInformation($"Found local sha: {sha}");
        return Task.FromResult(sha);
    }


    public override Task<string> GetRemoteResolvedRevision()
    {
        switch (SourceRevisionType)
        {
            case SourceRevisionType.Default:
                return Task.FromResult(SourceRevision);

            case SourceRevisionType.SemanticVersionRange:
                return Task.FromResult(_git.GetResolvedTag(SourceUrl, SourceRevision));
            default:
                throw new NotImplementedException($"Method {nameof(GetRemoteResolvedRevision)} has not been implemented for SourceRevisionType.{SourceRevisionType}.");
        }
    }

    public override Task<string> GetRemoteDefinitiveRevision()
    {
        var sha = _git.GetLatestRemoteSha(
            SourceUrl,
            SourceRevision,
            SourceRevisionType
        );

        if (string.IsNullOrEmpty(sha))
            Context.LogInformation($"No remote sha found");

        Context.LogInformation($"Found remote sha: {sha}");

        return Task.FromResult(sha);
    }

    protected override Task DownloadModule(string resolvedRevision)
    {
        _git.ShallowClone(
            ModuleDirectoryService.GetWorkingDir(),
            ModuleDirectoryService.GetModuleRootDir(),
            SourceUrl,
            resolvedRevision);

        var objectName =
            $"{SourceUrl}?ref={resolvedRevision}";

        Context.LogInformation(
            $"Shallow cloned {objectName} from source");
        return Task.CompletedTask;
    }
}