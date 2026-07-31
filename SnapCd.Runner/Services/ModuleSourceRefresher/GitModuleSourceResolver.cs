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
    private readonly ILogger<GitModuleSourceResolver> _logger;

    public GitModuleSourceResolver(ILogger<GitModuleSourceResolver> logger)
    {
        _logger = logger;
    }

    public string GetRemoteSemverRangeResolvedTag(string sourceUrl, string sourceRevision)
    {
        // An exact revision (no wildcards) is a literal tag: pure v?X.Y.Z, or any tag containing a fully
        // spelled-out version core (ui-v1.2.3, backend/v2.0.0). It needs no range resolution but stays valid
        // under SemanticVersionRange; revisions with no version core at all (branch names, bare majors) are
        // rejected so category errors still surface at parse time.
        if (!sourceRevision.Contains('*'))
        {
            if (Regex.IsMatch(sourceRevision, @"(?<![0-9])v?\d+\.\d+\.\d+(?![0-9])", RegexOptions.IgnoreCase))
                return sourceRevision;

            throw new ArgumentException($"Invalid semver range format: {sourceRevision}");
        }

        // Monorepos tag components with a discriminator around the version core (ui-v1.2.3, backend/v2.0.0,
        // 1.2.3-ui): the range is <prefix><v?X.* | v?X.Y.*><suffix>, where prefix and suffix are literal
        // anchors and the wildcard core carries the range semantics.
        var located = LocateWildcardCore(sourceRevision) ?? throw new ArgumentException($"Invalid semver range format: {sourceRevision}");
        var (prefix, core, suffix) = located;

        if (prefix.Contains('*') || suffix.Contains('*'))
            throw new ArgumentException($"Invalid semver range format: {sourceRevision}");

        var coreMatch = Regex.Match(core, @"^v?(?:(\d+)\.(?:(\d+)\.)?)?\*$", RegexOptions.IgnoreCase);
        int? requiredMajor = coreMatch.Groups[1].Success ? int.Parse(coreMatch.Groups[1].Value) : null;
        int? requiredMinor = coreMatch.Groups[2].Success ? int.Parse(coreMatch.Groups[2].Value) : null;

        // Match remote tags against the anchored shape. The optional v is part of the core and matches
        // case-insensitively whether or not the range spells it out; the anchors are case-sensitive literals.
        // Pre-release tags only ever match when the suffix spells the pre-release part out — the $ anchor
        // excludes them otherwise.
        var tagRegex = new Regex("^" + Regex.Escape(prefix) + @"(?i:v)?(\d+)\.(\d+)\.(\d+)" + Regex.Escape(suffix) + "$");

        var matchingTags = GetRemoteTags(sourceUrl)
            .Select(tag => new { Tag = tag, Match = tagRegex.Match(tag) })
            .Where(x => x.Match.Success)
            .Select(x => new SemverTag
            {
                Original = x.Tag,
                Major = int.Parse(x.Match.Groups[1].Value),
                Minor = int.Parse(x.Match.Groups[2].Value),
                Patch = int.Parse(x.Match.Groups[3].Value)
            })
            .Where(tag => (!requiredMajor.HasValue || tag.Major == requiredMajor.Value) &&
                          (!requiredMinor.HasValue || tag.Minor == requiredMinor.Value))
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

    /// <summary>
    /// Locates the wildcard version core (v?*, v?X.* or v?X.Y.*) inside a range expression: the core must be
    /// preceded by start-of-string or a non-digit and followed by end-of-string or a non-digit, so prefixes
    /// ending in digits (release2-1.*) stay unambiguous. The last such match wins when the prefix itself
    /// contains something version-shaped. The bare v?* core means "any version, later majors included".
    /// </summary>
    private static (string Prefix, string Core, string Suffix)? LocateWildcardCore(string revision)
    {
        var matches = Regex.Matches(revision, @"(?<![0-9])(?:v?\d+\.(?:\*|\d+\.\*)|v?\*)(?![0-9])", RegexOptions.IgnoreCase);
        if (matches.Count == 0) return null;

        var match = matches[^1];
        return (revision[..match.Index], match.Value, revision[(match.Index + match.Length)..]);
    }

    public string GetRemoteSemverRangeDefinitiveRevision(string sourceUrl, string sourceRevision)
    {
        var resolvedTag = GetRemoteSemverRangeResolvedTag(sourceUrl, sourceRevision);

        // Get the commit SHA for the resolved tag
        return GetRemoteDefaultDefinitiveRevision(sourceUrl, resolvedTag);
    }

    private class SemverTag
    {
        public required string Original { get; set; }
        public required int Major { get; set; }
        public required int Minor { get; set; }
        public required int Patch { get; set; }
    }

    private IEnumerable<string> GetRemoteTags(string sourceUrl)
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

            tags.Add(tagName);
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
                _logger.LogError("git ls-remote failed for {TargetRepoUrl}: {Error}", targetRepoUrl, error);
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