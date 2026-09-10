// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Telemetry;
using SnapCd.Server.Host.Installations;

namespace SnapCd.Server.Host.Telemetry;

/// <summary><paramref name="Bump"/> is what this release changed relative to the one before it: patch, minor or major.</summary>
public record ReleaseSummary(SemanticVersion Version, string Tag, string Heading, string Url, string Bump);

/// <summary>Releases newer than the running version, read from GitHub. Cached an hour in the distributed cache, so replicas share one fetch and a restart keeps it; unreachable means whatever is cached, else an empty list.</summary>
public class GitHubReleasesClient(
    IHttpClientFactory httpClientFactory,
    IOptions<TelemetrySettings> settings,
    IVersionService versionService,
    InstallationService installation,
    IDistributedCache cache,
    ILogger<GitHubReleasesClient> logger)
{
    private const string CacheKey = "telemetry:releases";
    private static readonly TimeSpan CacheFor = TimeSpan.FromHours(1);
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>False after a failed fetch with nothing cached, so the page can say GitHub was unreachable rather than "you are current".</summary>
    public bool LastFetchSucceeded { get; private set; } = true;

    private record GitHubRelease(string? tag_name, string? name, string? body, string? html_url, bool draft, bool prerelease);

    public async Task<IReadOnlyList<ReleaseSummary>> NewerThanRunningAsync(CancellationToken ct = default)
    {
        if (await ReadCacheAsync(ct) is { } cached) return cached;

        await _gate.WaitAsync(ct);
        try
        {
            if (await ReadCacheAsync(ct) is { } again) return again;

            var repo = settings.Value.ReleaseRepository;
            var client = httpClientFactory.CreateClient(nameof(GitHubReleasesClient));
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("snapcd-server");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var releases = await client.GetFromJsonAsync<List<GitHubRelease>>(
                $"https://api.github.com/repos/{repo}/releases?per_page=50", ct) ?? [];

            var list = Summarise(releases, versionService.ShortVersion, repo);
            await WriteCacheAsync(list, ct);
            LastFetchSucceeded = true;
            await RememberLatestAsync(releases, ct);
            return list;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Release list from GitHub unavailable");
            LastFetchSucceeded = false;
            return [];
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ReleaseSummary>?> ReadCacheAsync(CancellationToken ct)
    {
        try
        {
            var json = await cache.GetStringAsync(CacheKey, ct);
            return json is null ? null : JsonSerializer.Deserialize<List<ReleaseSummary>>(json);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Release cache unavailable; fetching");
            return null;
        }
    }

    private async Task WriteCacheAsync(List<ReleaseSummary> list, CancellationToken ct)
    {
        try
        {
            await cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(list),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheFor }, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Release cache could not be written");
        }
    }

    /// <summary>The newest non-draft release, newer than the running version or not, lands on the installation row for the nav indicator.</summary>
    private async Task RememberLatestAsync(IEnumerable<GitHubRelease> releases, CancellationToken ct)
    {
        var latest = releases
            .Where(r => !r.draft && SemanticVersion.TryParse(r.tag_name, out _))
            .Select(r => { SemanticVersion.TryParse(r.tag_name, out var v); return v; })
            .OrderByDescending(v => v)
            .FirstOrDefault();
        if (latest == default) return;
        try
        {
            await installation.UpdateAsync(row =>
            {
                row.LatestKnownVersion = latest.ToString();
                row.LatestKnownVersionCheckedAtUtc = DateTime.UtcNow;
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not store the latest known version");
        }
    }

    private static List<ReleaseSummary> Summarise(IEnumerable<GitHubRelease> releases, string running, string repo)
    {
        SemanticVersion.TryParse(running, out var current);
        var newer = releases
            .Where(r => !r.draft && SemanticVersion.TryParse(r.tag_name, out var v) && v > current)
            .Select(r => { SemanticVersion.TryParse(r.tag_name, out var v); return (Release: r, Version: v); })
            .OrderBy(x => x.Version)
            .ToList();

        var list = new List<ReleaseSummary>();
        var previous = current;
        foreach (var (r, v) in newer)
        {
            var tag = r.tag_name!.Trim();
            // html_url is the release's own page, stable however the list is paginated.
            var url = string.IsNullOrWhiteSpace(r.html_url) ? $"https://github.com/{repo}/releases/tag/{tag}" : r.html_url;
            list.Add(new ReleaseSummary(v, tag, Heading(r.body, r.name, tag), url, VersionDelta.Describe(previous.ToString(), tag) ?? "patch"));
            previous = v;
        }
        list.Reverse();
        return list;
    }

    /// <summary>First markdown heading of the notes; otherwise the first sentence; otherwise the release name or tag.</summary>
    public static string Heading(string? body, string? name, string tag)
    {
        var lines = (body ?? "").Replace("\r", "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var heading = lines.Select(l => Regex.Match(l, @"^#{1,6}\s+(.+?)\s*#*$")).FirstOrDefault(m => m.Success)?.Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(heading)) return Strip(heading);

        var first = lines.FirstOrDefault(l => !l.StartsWith("<!--") && !l.StartsWith("---"));
        if (first is not null)
        {
            var end = Regex.Match(first, @"[.!?](\s|$)");
            var sentence = end.Success ? first[..(end.Index + 1)] : first;
            return Strip(sentence);
        }

        return string.IsNullOrWhiteSpace(name) ? tag : name.Trim();
    }

    private static string Strip(string markdown) =>
        Regex.Replace(Regex.Replace(markdown, @"\[([^\]]+)\]\([^)]*\)", "$1"), @"[*_`]", "").Trim();
}
