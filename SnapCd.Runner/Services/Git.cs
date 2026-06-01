// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using System.Text.RegularExpressions;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services.ModuleSourceRefresher;

namespace SnapCd.Runner.Services;

public class Git
{
    private readonly ILogger<Git> _logger;
    private readonly RunnerTaskContext _context;
    private readonly GitModuleSourceResolver _sourceResolver;

    public Git(
        ILogger<Git> logger,
        RunnerTaskContext context,
        GitModuleSourceResolver sourceResolver
    )
    {
        _logger = logger;
        _context = context;
        _sourceResolver = sourceResolver;
    }


    public Task<HashSet<string>> GetWorkingFiles(string repoPath)
    {
        var files = new HashSet<string>(StringComparer.Ordinal);

        if (!Directory.Exists(repoPath))
            return Task.FromResult(files);

        try
        {
            // Files starting with .terraform*
            files.UnionWith(Directory.EnumerateFiles(repoPath, ".terraform*", SearchOption.AllDirectories));

            // State files (local backend)
            files.UnionWith(Directory.EnumerateFiles(repoPath, "*.tfstate*", SearchOption.AllDirectories));

            // Files in .terraform directories
            foreach (var dir in Directory.EnumerateDirectories(repoPath, ".terraform", SearchOption.AllDirectories)) files.UnionWith(Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories));

            // // Files in .snapcd directories
            // foreach (var dir in Directory.EnumerateDirectories(repoPath, ".snapcd", SearchOption.AllDirectories))
            // {
            //     files.UnionWith(Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories));
            // }
        }
        catch (Exception ex)
        {
            _context.LogWarning(ex.ToString());
            return Task.FromResult(new HashSet<string>(StringComparer.Ordinal));
        }

        return Task.FromResult(files);
    }

    public Task<HashSet<string>> GetNotTrackedGitFiles(string repoPath)
    {
        if (!Directory.Exists(repoPath)) return Task.FromResult(new HashSet<string>());

        try
        {
            var excludedFiles = new HashSet<string>();

            var command = "-c \"git ls-files --others --exclude-standard && git ls-files --others -i --exclude-standard\"";

            // Run the Git command to get excluded files
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = repoPath
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new Exception($"Git process failed with exit code {process.ExitCode}. Error: {error}");
                }

                // Normalize paths to absolute paths for comparison
                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    excludedFiles.Add(Path.GetFullPath(Path.Combine(repoPath, line)));
            }

            static List<string> ExpandIfDirectory(string path)
            {
                // For some reason the above will sometimes return a directory instead of the files inside the dir. This
                // function expands into 
                if (Directory.Exists(path))
                    return new List<string>(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
                return new List<string> { path };
            }

            var expandedFiles = new HashSet<string>();
            foreach (var path in excludedFiles) expandedFiles.UnionWith(ExpandIfDirectory(path));

            return Task.FromResult(expandedFiles);
        }
        catch (Exception ex)
        {
            _context.LogWarning(ex.ToString());
            return Task.FromResult(new HashSet<string>());
        }
    }

    public string GetLatestLocalSha(string repoPath)
    {
        if (!Directory.Exists(repoPath)) return "";

        try
        {
            var command = "git";
            var arguments = "rev-parse HEAD";

            using (var process = new Process())
            {
                process.StartInfo.WorkingDirectory = repoPath; // Set the working directory to the repo path
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true; // Redirect output
                process.StartInfo.UseShellExecute = false; // Required to redirect output
                process.StartInfo.CreateNoWindow = true; // No window

                process.Start();

                var output = process.StandardOutput.ReadToEnd(); // Read output
                process.WaitForExit(); // Wait for the process to exit

                return output.Trim(); // Return the SHA, trimming any extra whitespace
            }
        }
        catch (Exception ex)
        {
            _context.LogWarning(ex.ToString());
            return "";
        }
    }


    public string GetResolvedTag(string sourceUrl, string sourceRevision)
    {
        try
        {
            return _sourceResolver.GetRemoteSemverRangeResolvedTag(sourceUrl, sourceRevision);
        }
        catch (Exception ex)
        {
            _context.LogError(ex.ToString());
            throw;
        }
    }

    public string GetLatestRemoteSha(string sourceUrl, string sourceRevision, SourceRevisionType sourceRevisionType)
    {
        try
        {
            return _sourceResolver.GetRemoteDefinitiveRevision(sourceUrl, sourceRevision, sourceRevisionType);
        }
        catch (Exception ex)
        {
            _context.LogError(ex.ToString());
            throw;
        }
    }

    public void ShallowClone(string workingDir, string repoPath, string targetRepoUrl, string targetRepoRevision)
    {
        string arguments;
        if (targetRepoRevision == "")
        {
            arguments = $"-c \"git clone --depth 1 {targetRepoUrl} {repoPath} \"";
        }
        else if (!RevisionIsCommit(targetRepoRevision))
        {
            //treat "branch" and "tag" type revisions as a branch checkout
            arguments = $"-c \"git clone --depth 1 --branch {targetRepoRevision} {targetRepoUrl} {repoPath}\"";
        }
        else
        {
            // "commit" type revision require a different clone strategy
            arguments = $"-c \"git clone --depth 1 {targetRepoUrl} {repoPath} \n"; // chaining with &&
            arguments += $"cd {repoPath} \n"; // chaining with &&
            arguments += $"git fetch --depth 1 origin {targetRepoRevision} \n"; // no 'cd' needed
            arguments += $"git checkout {targetRepoRevision}\""; // no 'cd' needed
        }


        _context.LogInformation($"Running command: `/bin/bash {arguments}`");
        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();

        //NOTE that Git appears to *only* stream to "error", even if an error does not occur. https://namespaceoverflow.com/a/59941191
        var output = process.StandardError.ReadToEndAsync().GetAwaiter().GetResult();

        // We can however still rely on the exit code to know if an error actually occurred
        if (process.ExitCode != 0)
        {
            var err = $"Git failed with status code {process.ExitCode}. Message: {output}";
            _context.LogError(err);
            throw new Exception(err);
        }

        _context.LogInformation(output);
    }


    public bool RevisionIsCommit(string targetRepoRevision)
    {
        return Regex.IsMatch(targetRepoRevision, "^[0-9a-f]{40}$", RegexOptions.IgnoreCase);
    }
}