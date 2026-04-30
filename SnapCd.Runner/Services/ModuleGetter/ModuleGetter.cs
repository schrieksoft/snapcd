using System.Text.Json;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Services.ModuleGetter;

public abstract class ModuleGetter
{
    protected readonly string TempWorkingFilesStashDir;
    protected readonly string SourceUrl;
    protected readonly string SourceRevision;
    protected readonly string Subdirectory;
    protected readonly SourceRevisionType SourceRevisionType;
    protected readonly RunnerTaskContext Context;
    protected readonly ILogger<ModuleGetter> Logger;
    protected readonly ModuleDirectoryService ModuleDirectoryService;
    protected readonly string Engine;
    protected string ResolvedSourceRevision = "";


    protected ModuleGetter(
        SourceRevisionType sourceRevisionType,
        string sourceUrl,
        string sourceRevision,
        string? subdirectory,
        ModuleDirectoryService moduleDirectoryService,
        RunnerTaskContext context,
        ILogger<ModuleGetter> logger,
        string engine
    )
    {
        SourceUrl = sourceUrl;
        SourceRevision = sourceRevision;
        Subdirectory = subdirectory ?? string.Empty;
        SourceRevisionType = sourceRevisionType;
        Engine = engine;

        Context = context;
        Logger = logger;
        ModuleDirectoryService = moduleDirectoryService;

        TempWorkingFilesStashDir = Path.Combine(ModuleDirectoryService.GetTempDir(), "stash", "workingfiles");

        CreateDirectoryIfNotExists(ModuleDirectoryService.GetWorkingDir());
        CreateDirectoryIfNotExists(ModuleDirectoryService.GetTempDir());
        CreateDirectoryIfNotExists(TempWorkingFilesStashDir);
        CreateDirectoryIfNotExists(ModuleDirectoryService.GetModuleRootDir());
    }


    // abstract methods

    protected abstract Task<HashSet<string>> GetWorkingFiles();
    protected abstract Task<string> StashWorkingFiles(HashSet<string> workingFilePaths);
    protected abstract Task UnstashWorkingFiles(HashSet<string> workingFilePaths, string tempDir);
    protected abstract Task<string> GetLocalDefinitiveRevision();
    public abstract Task<string> GetRemoteDefinitiveRevision();
    public abstract Task<string> GetRemoteResolvedRevision();
    protected abstract Task DownloadModule(string resolvedRevision);

    protected abstract Task<bool> ConfirmModuleDownloaded();


    // concrete methods


    protected virtual Task Init()
    {
        return Task.CompletedTask;
    }


    protected virtual Task CreateSnapCdDir()
    {
        // Create the directory if it doesn't exist
        if (!Directory.Exists(ModuleDirectoryService.GetSnapCdDir()))
            Directory.CreateDirectory(ModuleDirectoryService.GetSnapCdDir());

        // Path to the .gitignore file
        var gitignorePath = Path.Combine(ModuleDirectoryService.GetSnapCdDir(), ".gitignore");

        // Write the .gitignore file to ignore everything, including itself
        File.WriteAllText(gitignorePath, "*\n");
        return Task.CompletedTask;
    }

    protected virtual async Task CleanupTemporaryFiles(string tempDir)
    {
        Context.LogInformation("Cleaning up temporary files");
        await DeleteDirectoryIfExists(tempDir);
    }

    public async Task GetModule(
        bool cleanInitEnabled,
        List<ExtraFileDto>? extraFiles,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default,
        string? remoteDefinitiveRevision = null)
    {
        await Init();
        var remoteResolvedRevision = await GetRemoteResolvedRevision();
        var localDefinitiveRevision = await GetLocalDefinitiveRevision();
        if (remoteDefinitiveRevision == null)
            remoteDefinitiveRevision = await GetRemoteDefinitiveRevision();

        var workingFilePaths = new HashSet<string>();
        var tempDir = string.Empty;

        if (!cleanInitEnabled)
        {
            workingFilePaths = await GetWorkingFiles();
            tempDir = await StashWorkingFiles(workingFilePaths);
        }

        try
        {
            if (localDefinitiveRevision != remoteDefinitiveRevision || localDefinitiveRevision == "")
            {
                Context.LogInformation(
                    !cleanInitEnabled
                        ? $"CleanInitEnabled set to 'false`. Stashing existing .terraform* files before redownloading module"
                        : $"Clean Init has been enabled. No existing files will be kept, module will be completely redownloaded.");

                await DeleteModuleRootDirIfExists();
                await DownloadModule(remoteResolvedRevision);
                if (!await ConfirmModuleDownloaded())
                    throw new Exception($"Module {ModuleDirectoryService.GetModuleRootDir()} not properly downloaded");
                if (!cleanInitEnabled)
                    await UnstashWorkingFiles(workingFilePaths, tempDir);
            }
            else
            {
                Context.LogInformation(
                    $"Local SHA equals latest remote SHA ({remoteDefinitiveRevision}), continuing with current local revision.");
            }

            await CreateSnapCdDir();
        }
        finally
        {
            await CleanupTemporaryFiles(tempDir);
        }

        await AddExtraFiles(workingFilePaths, extraFiles);
    }

    // concrete protected methods

    protected void CreateDirectoryIfNotExists(string directory)
    {
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
    }


    protected virtual async Task AddExtraFiles(HashSet<string> workingFilePaths, List<ExtraFileDto>? extraFiles)
    {
        var manifestPath = Path.Combine(ModuleDirectoryService.GetSnapCdDir(), "extra-files.json");
        var backupDir = Path.Combine(ModuleDirectoryService.GetSnapCdDir(), "original-files");
        var previousManifest = await ReadExtraFilesManifest(manifestPath);
        var currentExtraFileNames = extraFiles?.Select(f => f.FileName).ToHashSet() ?? new HashSet<string>();
        var newManifest = new ExtraFilesManifest();

        // Cleanup: delete created files and restore overwritten files that are no longer in the extra files list
        foreach (var createdFileName in previousManifest.Created)
        {
            if (!currentExtraFileNames.Contains(createdFileName))
            {
                var path = Path.Combine(ModuleDirectoryService.GetInitDir(), createdFileName);
                if (File.Exists(path))
                {
                    Context.LogInformation($"Removing extra file: {createdFileName}");
                    File.Delete(path);
                }
            }
        }

        foreach (var overwrittenFileName in previousManifest.Overwritten)
        {
            if (!currentExtraFileNames.Contains(overwrittenFileName))
            {
                var path = Path.Combine(ModuleDirectoryService.GetInitDir(), overwrittenFileName);
                await RestoreOriginalFile(backupDir, overwrittenFileName, path);
            }
        }

        // Write current extra files
        if (extraFiles != null && extraFiles.Count > 0)
        {
            Context.LogInformation($"Now adding extra files to folder \"{ModuleDirectoryService.GetInitDir()}\"");
            foreach (var file in extraFiles)
            {
                var path = Path.Combine(ModuleDirectoryService.GetInitDir(), file.FileName);

                if (File.Exists(path) && workingFilePaths.Contains(path))
                {
                    // Working files (terraform state) - always write, don't track
                    await File.WriteAllTextAsync(path, file.Contents);
                }
                else if (File.Exists(path))
                {
                    // Source file exists
                    if (file.Overwrite)
                    {
                        await BackupOriginalFile(backupDir, file.FileName, path);
                        await File.WriteAllTextAsync(path, file.Contents);
                        newManifest.Overwritten.Add(file.FileName);
                    }
                    // else: Overwrite=false, skip and don't track
                }
                else
                {
                    // File doesn't exist - create it
                    await File.WriteAllTextAsync(path, file.Contents);
                    newManifest.Created.Add(file.FileName);
                }
            }
        }

        // Save new manifest
        await WriteExtraFilesManifest(manifestPath, newManifest);
    }

    private async Task BackupOriginalFile(string backupDir, string fileName, string sourcePath)
    {
        var backupPath = Path.Combine(backupDir, fileName);

        // Only backup if not already backed up (from a previous run)
        if (File.Exists(backupPath))
            return;

        CreateDirectoryIfNotExists(backupDir);
        await File.WriteAllBytesAsync(backupPath, await File.ReadAllBytesAsync(sourcePath));
        Context.LogInformation($"Backed up original file: {fileName}");
    }

    private async Task RestoreOriginalFile(string backupDir, string fileName, string targetPath)
    {
        var backupPath = Path.Combine(backupDir, fileName);

        if (File.Exists(backupPath))
        {
            await File.WriteAllBytesAsync(targetPath, await File.ReadAllBytesAsync(backupPath));
            File.Delete(backupPath);
            Context.LogInformation($"Restored original file: {fileName}");
        }
        else
        {
            // Backup missing - delete the overwritten file
            if (File.Exists(targetPath))
            {
                Context.LogWarning($"Backup not found for {fileName}, deleting overwritten file");
                File.Delete(targetPath);
            }
        }
    }

    private async Task<ExtraFilesManifest> ReadExtraFilesManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            return new ExtraFilesManifest();

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            return JsonSerializer.Deserialize<ExtraFilesManifest>(json) ?? new ExtraFilesManifest();
        }
        catch (Exception ex)
        {
            Context.LogWarning($"Failed to read extra files manifest: {ex.Message}");
            return new ExtraFilesManifest();
        }
    }

    private async Task WriteExtraFilesManifest(string manifestPath, ExtraFilesManifest manifest)
    {
        var json = JsonSerializer.Serialize(manifest);
        await File.WriteAllTextAsync(manifestPath, json);
    }

    private class ExtraFilesManifest
    {
        public List<string> Created { get; set; } = new();
        public List<string> Overwritten { get; set; } = new();
    }

    protected virtual async Task DeleteModuleRootDirIfExists()
    {
        await DeleteDirectoryIfExists(ModuleDirectoryService.GetModuleRootDir());
    }

    protected virtual Task DeleteDirectoryIfExists(string directory)
    {
        try
        {
            // Check if the directory exists
            if (Directory.Exists(directory))
            {
                // Delete the directory and its contents
                Directory.Delete(directory, true);
                Context.LogInformation($"Directory {directory} deleted successfully");
            }
            else
            {
                Context.LogInformation($"Directory {directory} does not exist, doing nothing");
            }
        }
        catch (Exception ex)
        {
            // Handle any errors that might occur
            Context.LogError($"An unexpected error occured deleting project directory: \n {ex.Message}");
            throw new Exception($"An unexpected error occured deleting project directory: \n {ex.Message}");
        }

        return Task.CompletedTask;
    }
}