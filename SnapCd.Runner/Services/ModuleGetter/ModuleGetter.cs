// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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

    /// <summary>
    /// Returns the existing checkout to the state the source shipped. Engine artifacts are kept
    /// unless <paramref name="includeEngineArtifacts"/> says otherwise.
    /// </summary>
    protected abstract Task ResetToSource(bool includeEngineArtifacts);


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
        await DeleteDirectoryIfExists(tempDir);
    }

    public async Task GetModule(
        bool cleanInitEnabled,
        List<ExtraFileDto>? extraFiles,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default,
        string? remoteDefinitiveRevision = null)
    {
        // This task runs no external process, so the blank line that would precede a command's
        // output has to be emitted here instead.
        Context.LogBreak();

        Context.LogInformation($"Module directory: {Ansi.Emphasis(ModuleDirectoryService.GetInitDir())}");

        await Init();
        var remoteResolvedRevision = await GetRemoteResolvedRevision();
        var localDefinitiveRevision = await GetLocalDefinitiveRevision();
        if (remoteDefinitiveRevision == null)
            remoteDefinitiveRevision = await GetRemoteDefinitiveRevision();

        var workingFilePaths = new HashSet<string>();
        var tempDir = string.Empty;

        if (localDefinitiveRevision != remoteDefinitiveRevision || localDefinitiveRevision == "")
        {
            // A different revision is a different tree, so the checkout is replaced rather than
            // reset. The engine artifacts are carried across it: they belong to the module, not to
            // the revision, and fetching them again costs a provider download per run. Clean init
            // is the request not to.
            if (!cleanInitEnabled)
            {
                workingFilePaths = await GetWorkingFiles();
                tempDir = await StashWorkingFiles(workingFilePaths);
            }

            try
            {
                await DeleteModuleRootDirIfExists();
                await DownloadModule(remoteResolvedRevision);
                if (!await ConfirmModuleDownloaded())
                    throw new Exception($"Module {ModuleDirectoryService.GetModuleRootDir()} not properly downloaded");
                if (!cleanInitEnabled)
                    await UnstashWorkingFiles(workingFilePaths, tempDir);
            }
            finally
            {
                await CleanupTemporaryFiles(tempDir);
            }
        }
        else
        {
            // Same revision, so the checkout is reused. Resetting it is what removes whatever the
            // last run wrote into it - extra files it created, source files it overwrote - so the
            // tree matches the source before anything is written on top of it again.
            Context.LogInformation(
                $"Local revision matches remote {Ansi.Emphasis(remoteDefinitiveRevision)}; resetting the checkout");

            // Snap CD's own directory is gitignored, so the reset leaves it: remove it explicitly
            // so nothing from the previous run is read as if this run had produced it.
            await DeleteDirectoryIfExists(ModuleDirectoryService.GetSnapCdDir());
            await ResetToSource(cleanInitEnabled);
        }

        await CreateSnapCdDir();

        await AddExtraFiles(workingFilePaths, extraFiles);
    }

    // concrete protected methods

    protected void CreateDirectoryIfNotExists(string directory)
    {
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
    }


    protected virtual async Task AddExtraFiles(HashSet<string> workingFilePaths, List<ExtraFileDto>? extraFiles)
    {
        if (extraFiles == null || extraFiles.Count == 0)
            return;

        var written = new List<string>();
        var overwritten = new List<string>();
        var skipped = new List<string>();

        foreach (var file in extraFiles)
        {
            var path = Path.Combine(ModuleDirectoryService.GetInitDir(), file.FileName);

            // The checkout matches the source at this point, so a file already at this path is one
            // the source ships and Overwrite is the answer to whether it may be replaced. Working
            // files are the engine's own state, carried across the download, and are always written.
            if (File.Exists(path) && !workingFilePaths.Contains(path))
            {
                if (!file.Overwrite)
                {
                    skipped.Add(file.FileName);
                    continue;
                }

                overwritten.Add(file.FileName);
            }
            else
            {
                written.Add(file.FileName);
            }

            await File.WriteAllTextAsync(path, file.Contents);
        }

        Context.LogInformation("Now adding extra files");
        foreach (var fileName in written)
            Context.LogInformation($"  {Ansi.Emphasis(fileName)}");
        foreach (var fileName in overwritten)
            Context.LogInformation($"  {Ansi.Emphasis(fileName)} (overwrote the source's own)");
        foreach (var fileName in skipped)
            Context.LogInformation($"  {Ansi.Emphasis(fileName)} (skipped: the source has its own and overwrite is off)");
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
            }
            else
            {
                Context.LogDebug($"Directory {directory} does not exist, doing nothing");
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