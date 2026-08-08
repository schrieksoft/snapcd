// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services.ModuleSourceRefresher;

/// <summary>
/// Maintains bare git clones per source URL so path-aware refreshes can read tree hashes without ever
/// materializing a working tree. Clones are created on first touch (optionally blob-filtered, see
/// SourceCacheSettings.BlobFilterEnabled), fetched only when the requested commit is not yet present,
/// serialized per URL, and evicted least-recently-used when the cache exceeds its size cap.
/// </summary>
public class BareCloneCache
{
    private const string LastUsedMarker = ".snapcd-last-used";

    private readonly string _cacheRoot;
    private readonly SourceCacheSettings _settings;
    private readonly ILogger<BareCloneCache> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public BareCloneCache(
        IOptions<WorkingDirectorySettings> workingDirectorySettings,
        IOptions<SourceCacheSettings> settings,
        ILogger<BareCloneCache> logger)
        : this(Path.Combine(workingDirectorySettings.Value.WorkingDirectory, "sourcecache"), settings.Value, logger)
    {
    }

    public BareCloneCache(string cacheRoot, SourceCacheSettings settings, ILogger<BareCloneCache> logger)
    {
        _cacheRoot = cacheRoot;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Returns one PathHash per requested path: the git tree (or blob) object hash of that path at the given
    /// commit, or an empty TreeHash when the path does not exist at that commit. Paths are repo-root-relative;
    /// "." means the repository root tree.
    /// </summary>
    /// <summary>
    /// Ensures the clone for the URL exists and contains the commit, and returns the clone directory — for
    /// callers that need to run further git-based tooling (e.g. snapcd-inspect) against it.
    /// </summary>
    public Task<string> GetClonePath(string sourceUrl, string commitSha)
    {
        return EnsureCommit(sourceUrl, commitSha);
    }

    public async Task<List<PathHash>> GetTreeHashes(string sourceUrl, string commitSha, IReadOnlyCollection<string> paths)
    {
        var cloneDir = await EnsureCommit(sourceUrl, commitSha);

        var normalized = paths
            .Select(p => p.Trim().TrimEnd('/'))
            .Select(p => p.Length == 0 ? "." : p)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);

        var treePaths = normalized.Where(p => p != ".").ToList();
        if (treePaths.Count > 0)
        {
            var args = new List<string> { "ls-tree", commitSha, "--" };
            args.AddRange(treePaths);
            var output = RunGit(cloneDir, args);

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var tabIndex = line.IndexOf('\t');
                if (tabIndex < 0) continue;
                var path = line[(tabIndex + 1)..];
                var meta = line[..tabIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (meta.Length < 3) continue;
                hashes[path] = meta[2];
            }
        }

        if (normalized.Contains("."))
            hashes["."] = RunGit(cloneDir, new List<string> { "rev-parse", $"{commitSha}^{{tree}}" }).Trim();

        return normalized
            .Select(p => new PathHash { Path = p, TreeHash = hashes.GetValueOrDefault(p, "") })
            .ToList();
    }

    private async Task<string> EnsureCommit(string sourceUrl, string commitSha)
    {
        var key = CacheKey(sourceUrl);
        var cloneDir = Path.Combine(_cacheRoot, key);
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            if (!Directory.Exists(cloneDir))
                Clone(sourceUrl, cloneDir);

            if (!CommitExists(cloneDir, commitSha))
            {
                Fetch(cloneDir);

                if (!CommitExists(cloneDir, commitSha))
                    throw new Exception($"Commit {commitSha} not found in {sourceUrl} after fetch");
            }

            File.WriteAllText(Path.Combine(cloneDir, LastUsedMarker), DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            EvictIfOverCap(keepKey: key);

            return cloneDir;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void Clone(string sourceUrl, string cloneDir)
    {
        Directory.CreateDirectory(_cacheRoot);

        if (_settings.BlobFilterEnabled)
            try
            {
                RunGit(_cacheRoot, new List<string> { "clone", "--bare", "--filter=blob:none", sourceUrl, cloneDir });
                return;
            }
            catch (Exception ex)
            {
                // Not every git host supports partial clone; fall back to a full bare clone.
                _logger.LogWarning(ex, "Blob-filtered clone of {SourceUrl} failed, retrying without filter", sourceUrl);
                if (Directory.Exists(cloneDir)) Directory.Delete(cloneDir, true);
            }

        RunGit(_cacheRoot, new List<string> { "clone", "--bare", sourceUrl, cloneDir });
    }

    private void Fetch(string cloneDir)
    {
        RunGit(cloneDir, new List<string> { "fetch", "--force", "--prune", "origin", "+refs/heads/*:refs/heads/*", "+refs/tags/*:refs/tags/*" });
    }

    private bool CommitExists(string cloneDir, string commitSha)
    {
        try
        {
            RunGit(cloneDir, new List<string> { "cat-file", "-e", $"{commitSha}^{{commit}}" });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void EvictIfOverCap(string keepKey)
    {
        if (_settings.MaxSizeMb <= 0) return;

        var capBytes = (long)_settings.MaxSizeMb * 1024 * 1024;
        var clones = Directory.EnumerateDirectories(_cacheRoot)
            .Select(dir => new
            {
                Dir = dir,
                Size = GetDirectorySize(dir),
                LastUsed = ReadLastUsed(dir)
            })
            .ToList();

        var totalSize = clones.Sum(c => c.Size);

        foreach (var clone in clones.OrderBy(c => c.LastUsed))
        {
            if (totalSize <= capBytes) break;
            if (Path.GetFileName(clone.Dir) == keepKey) continue;

            _logger.LogDebug("Evicting bare clone {Dir} ({SizeMb} MB, last used {LastUsed})",
                clone.Dir, clone.Size / (1024 * 1024), DateTimeOffset.FromUnixTimeSeconds(clone.LastUsed));
            Directory.Delete(clone.Dir, true);
            totalSize -= clone.Size;
        }
    }

    private static long ReadLastUsed(string cloneDir)
    {
        var marker = Path.Combine(cloneDir, LastUsedMarker);
        if (File.Exists(marker) && long.TryParse(File.ReadAllText(marker), out var timestamp))
            return timestamp;
        return 0;
    }

    private static long GetDirectorySize(string dir)
    {
        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
    }

    private static string CacheKey(string sourceUrl)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sourceUrl.Trim()));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string RunGit(string workingDirectory, List<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"git {string.Join(' ', arguments)} failed with exit code {process.ExitCode}: {stderr}");

        return stdout;
    }
}
