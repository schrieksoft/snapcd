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

namespace SnapCd.Runner.Services.ModuleSourceRefresher;

public class GitModuleSourceResolver : IModuleSourceRefresher
{
    public string GetRemoteSemverRangeResolvedTag(string sourceUrl, string sourceRevision)
    {
        // Check if the sourceRevision is an exact version (no wildcards)
        var exactVersionRegex = new Regex(@"^v?\d+\.\d+\.\d+$");
        if (exactVersionRegex.IsMatch(sourceRevision)) return sourceRevision;

        // Check if sourceRevision contains a wildcard
        if (!sourceRevision.Contains('*')) throw new ArgumentException($"Invalid semver range format: {sourceRevision}");

        // Parse the range by removing leading 'v' case-insensitively
        var rangeWithoutV = sourceRevision.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? sourceRevision.Substring(1)
            : sourceRevision;

        var rangeParts = rangeWithoutV.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

        int requiredMajor;
        int? requiredMinor = null;

        if (rangeParts.Length == 2 && rangeParts[1] == "*")
        {
            if (!int.TryParse(rangeParts[0], out requiredMajor)) throw new ArgumentException($"Invalid major version in range: {sourceRevision}");
        }
        else if (rangeParts.Length == 3 && rangeParts[2] == "*")
        {
            if (!int.TryParse(rangeParts[0], out requiredMajor) || !int.TryParse(rangeParts[1], out var minor)) throw new ArgumentException($"Invalid minor version in range: {sourceRevision}");
            requiredMinor = minor;
        }
        else
        {
            throw new ArgumentException($"Invalid semver range format: {sourceRevision}");
        }

        // Fetch all remote semver tags
        var remoteTags = GetRemoteVersionTags(sourceUrl)
            .Select(tag => ParseSemverTag(tag))
            .Where(tag => tag != null)
            .Cast<SemverTag>()
            .ToList();

        if (remoteTags.Count == 0) throw new Exception($"No valid semantic version tags found in remote repository {sourceUrl}");

        // Filter tags that match the range
        var matchingTags = remoteTags
            .Where(tag =>
            {
                if (requiredMinor.HasValue)
                    return tag.Major == requiredMajor && tag.Minor == requiredMinor.Value;
                else
                    return tag.Major == requiredMajor;
            })
            .ToList();

        if (matchingTags.Count == 0) throw new Exception($"No tags in remote repository {sourceUrl} match the range {sourceRevision}");

        // Find the highest version
        var highestVersion = matchingTags
            .OrderByDescending(t => t.Major)
            .ThenByDescending(t => t.Minor)
            .ThenByDescending(t => t.Patch)
            .First();

        return highestVersion.Original;
    }

    public string GetRemoteSemverRangeDefinitiveRevision(string sourceUrl, string sourceRevision)
    {
        var resolvedTag = GetRemoteSemverRangeResolvedTag(sourceUrl, sourceRevision);

        // Get the commit SHA for the resolved tag
        return GetRemoteDefaultDefinitiveRevision(sourceUrl, resolvedTag);
    }

    private SemverTag? ParseSemverTag(string tag)
    {
        var tagWithoutV = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag.Substring(1) : tag;
        var parts = tagWithoutV.Split('.');
        if (parts.Length != 3) return null;

        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
            return null;

        return new SemverTag
        {
            Original = tag,
            Major = major,
            Minor = minor,
            Patch = patch
        };
    }

    private class SemverTag
    {
        public required string Original { get; set; }
        public required int Major { get; set; }
        public required int Minor { get; set; }
        public required int Patch { get; set; }
    }

    private IEnumerable<string> GetRemoteVersionTags(string sourceUrl)
    {
        var process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = $"ls-remote --tags {sourceUrl}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0) throw new Exception($"Failed to retrieve tags from remote: {error}");

        var tags = new List<string>();
        var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(new[] { '\t' }, 2);
            if (parts.Length != 2)
                continue;

            var refName = parts[1];
            if (!refName.StartsWith("refs/tags/"))
                continue;

            var tagName = refName.Substring("refs/tags/".Length);
            // Check for peeled tag
            if (tagName.EndsWith("^{}")) tagName = tagName.Substring(0, tagName.Length - 3);

            // Check if it's a valid semver tag (vX.Y.Z or X.Y.Z)
            if (Regex.IsMatch(tagName, @"^v?\d+\.\d+\.\d+$")) tags.Add(tagName);
        }

        // Remove duplicates (in case of peeled tags)
        return tags.Distinct();
    }


    public string GetRemoteDefaultDefinitiveRevision(string sourceUrl, string sourceRevision)
    {
        var command = "git";
        var arguments = $"ls-remote {sourceUrl} {sourceRevision}";

        using (var process = new Process())
        {
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true; // Redirect standard output
            process.StartInfo.RedirectStandardError = true; // Redirect standard error
            process.StartInfo.UseShellExecute = false; // Required to redirect
            process.StartInfo.CreateNoWindow = true; // No console window

            process.Start();

            var output = process.StandardOutput.ReadToEnd(); // Read standard output
            var errorOutput = process.StandardError.ReadToEnd(); // Read standard error
            process.WaitForExit(); // Wait for the process to exit

            if (errorOutput != "")
            {
                var err =
                    $"Unable to determine latest remote sha for \"{sourceUrl}\" at target revision \"{sourceRevision}\". Internal git error: \n {errorOutput}";
                throw new Exception(err);
            }

            if (output == "")
            {
                if (RevisionIsCommit(sourceRevision) && CommitExistsInRemote(sourceUrl, sourceRevision)) return sourceRevision;
                var err =
                    $"Unable to determine latest remote sha for \"{sourceUrl}\" at target revision \"{sourceRevision}\". Git did not provide an internal error, but returned a blank response to the `{command} {arguments}` command. This might mean it was able to connect to the repository at \"{sourceUrl}\", but could not find the revision \"{sourceRevision}\"";

                throw new Exception(err);
            }

            // Parse the output to get the commit SHA
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 0 ? lines[0].Split('\t')[0] : string.Empty; // Return the SHA or empty string
        }
    }


    public string GetRemoteDefinitiveRevision(string sourceUrl, string sourceRevision, SourceRevisionType sourceRevisionType)
    {
        switch (sourceRevisionType)
        {
            case SourceRevisionType.Default:
                return GetRemoteDefaultDefinitiveRevision(sourceUrl, sourceRevision);
            case SourceRevisionType.SemanticVersionRange:
                return GetRemoteSemverRangeDefinitiveRevision(sourceUrl, sourceRevision);
            default:
                return GetRemoteDefaultDefinitiveRevision(sourceUrl, sourceRevision);
        }
    }

    public bool RevisionIsCommit(string targetRepoRevision)
    {
        return Regex.IsMatch(targetRepoRevision, "^[0-9a-f]{40}$", RegexOptions.IgnoreCase);
    }

    private bool CommitExistsInRemote(string targetRepoUrl, string targetRepoRevision)
    {
        var commitExists = false;
        using (var process = new Process())
        {
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = $"ls-remote {targetRepoUrl}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // Handle error (e.g., remote not found)
                Console.WriteLine($"Error: {error}");
            }
            else
            {
                // Check each line for the SHA
                var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                commitExists = lines.Any(line => line.StartsWith(targetRepoRevision));
            }
        }

        return commitExists;
    }
}