// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Runner.Services.ModuleSourceRefresher;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tests;

public class BareCloneCacheTests : IDisposable
{
    private readonly string _root;
    private readonly string _sourceRepo;
    private readonly BareCloneCache _cache;

    public BareCloneCacheTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "snapcd-bareclonecache-tests", Guid.NewGuid().ToString("N"));
        _sourceRepo = Path.Combine(_root, "source");
        Directory.CreateDirectory(_sourceRepo);

        RunGit(_sourceRepo, "init", "-q", "-b", "main");
        RunGit(_sourceRepo, "config", "user.name", "Test");
        RunGit(_sourceRepo, "config", "user.email", "test@example.com");

        WriteFile("modules/app-a/main.tf", "output \"a\" { value = \"a\" }\n");
        WriteFile("shared/network/main.tf", "output \"n\" { value = \"n\" }\n");
        Commit("baseline");

        _cache = new BareCloneCache(Path.Combine(_root, "cache"), new SourceCacheSettings(), NullLogger<BareCloneCache>.Instance);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
            // best effort
        }
    }

    [Fact]
    public async Task Returns_Tree_Hashes_For_Existing_Paths()
    {
        var head = Head();

        var hashes = await _cache.GetTreeHashes(_sourceRepo, head, new[] { "modules/app-a", "shared/network" });

        var byPath = hashes.ToDictionary(h => h.Path, h => h.TreeHash);
        Assert.Equal(RunGit(_sourceRepo, "rev-parse", $"{head}:modules/app-a").Trim(), byPath["modules/app-a"]);
        Assert.Equal(RunGit(_sourceRepo, "rev-parse", $"{head}:shared/network").Trim(), byPath["shared/network"]);
    }

    [Fact]
    public async Task Missing_Path_Reports_Empty_Sentinel()
    {
        var hashes = await _cache.GetTreeHashes(_sourceRepo, Head(), new[] { "does/not/exist", "modules/app-a" });

        var byPath = hashes.ToDictionary(h => h.Path, h => h.TreeHash);
        Assert.Equal("", byPath["does/not/exist"]);
        Assert.NotEqual("", byPath["modules/app-a"]);
    }

    [Fact]
    public async Task Root_Path_Returns_Commit_Tree()
    {
        var head = Head();

        var hashes = await _cache.GetTreeHashes(_sourceRepo, head, new[] { "." });

        Assert.Equal(RunGit(_sourceRepo, "rev-parse", $"{head}^{{tree}}").Trim(), hashes.Single().TreeHash);
    }

    [Fact]
    public async Task Trailing_Slash_Is_Normalized()
    {
        var hashes = await _cache.GetTreeHashes(_sourceRepo, Head(), new[] { "modules/app-a/" });

        Assert.Equal("modules/app-a", hashes.Single().Path);
        Assert.NotEqual("", hashes.Single().TreeHash);
    }

    [Fact]
    public async Task Fetches_When_Commit_Not_Yet_Present()
    {
        var firstHead = Head();
        await _cache.GetTreeHashes(_sourceRepo, firstHead, new[] { "modules/app-a" });

        WriteFile("modules/app-a/main.tf", "output \"a\" { value = \"a2\" }\n");
        Commit("change app-a");
        var secondHead = Head();

        var hashes = await _cache.GetTreeHashes(_sourceRepo, secondHead, new[] { "modules/app-a", "shared/network" });

        var byPath = hashes.ToDictionary(h => h.Path, h => h.TreeHash);
        Assert.Equal(RunGit(_sourceRepo, "rev-parse", $"{secondHead}:modules/app-a").Trim(), byPath["modules/app-a"]);
        Assert.Equal(RunGit(_sourceRepo, "rev-parse", $"{firstHead}:shared/network").Trim(), byPath["shared/network"]);
    }

    [Fact]
    public async Task Unknown_Commit_Throws_After_Fetch()
    {
        await Assert.ThrowsAsync<Exception>(() =>
            _cache.GetTreeHashes(_sourceRepo, "0123456789012345678901234567890123456789", new[] { "modules/app-a" }));
    }

    private void WriteFile(string relativePath, string contents)
    {
        var fullPath = Path.Combine(_sourceRepo, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
    }

    private void Commit(string message)
    {
        RunGit(_sourceRepo, "add", "-A");
        RunGit(_sourceRepo, "commit", "-q", "-m", message);
    }

    private string Head()
    {
        return RunGit(_sourceRepo, "rev-parse", "HEAD").Trim();
    }

    private static string RunGit(string workingDirectory, params string[] arguments)
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
            throw new Exception($"git {string.Join(' ', arguments)} failed: {stderr}");

        return stdout;
    }
}

/// <summary>
/// Live tests against the public monorepo-testing fixture repo. The pinned values come from the fixture's frozen
/// refs (see the repo's README for the append/regenerate contract).
/// </summary>
public class BareCloneCacheFixtureRepoTests : IDisposable
{
    private const string FixtureUrl = "https://github.com/schrieksoft/monorepo-testing.git";
    private const string MainHead = "6051e6b0d42fbdb3141ef02990c6aa343d0e790c";
    private const string SameTreeHead = "af65fba90f85ca9bf04954aff14f1daa331aa3a1";

    private readonly string _root;
    private readonly BareCloneCache _cache;

    public BareCloneCacheFixtureRepoTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "snapcd-bareclonecache-tests", Guid.NewGuid().ToString("N"));
        _cache = new BareCloneCache(Path.Combine(_root, "cache"), new SourceCacheSettings(), NullLogger<BareCloneCache>.Instance);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
            // best effort
        }
    }

    [Theory]
    [InlineData("modules/app-a", "e9987d4183b227ccea0ddefc86c98545a60c8759")]
    [InlineData("shared/network", "9b37854015270c966516754db4c2b491e107f7b3")]
    [InlineData("shared/naming", "23c6bc795c9dac8b4f2efe07f7a0b188796d1dec")]
    [InlineData("docs", "c047bf192c669fd9f740e9e0ab9f599daf0562c2")]
    public async Task Resolves_Pinned_Tree_Hashes_At_Main(string path, string expectedTreeHash)
    {
        var hashes = await _cache.GetTreeHashes(FixtureUrl, MainHead, new[] { path });

        Assert.Equal(expectedTreeHash, hashes.Single().TreeHash);
    }

    [Fact]
    public async Task Identical_Trees_Under_Different_History_Hash_Identically()
    {
        var paths = new[] { "modules/app-a", "shared/network", "shared/naming", "docs" };

        var atMain = await _cache.GetTreeHashes(FixtureUrl, MainHead, paths);
        var atRewritten = await _cache.GetTreeHashes(FixtureUrl, SameTreeHead, paths);

        Assert.Equal(
            atMain.ToDictionary(h => h.Path, h => h.TreeHash),
            atRewritten.ToDictionary(h => h.Path, h => h.TreeHash));
    }
}
