// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SnapCd.Runner.Services.ModuleSourceRefresher;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tests;

public class SnapCdInspectTests : IDisposable
{
    private readonly string _root;

    public SnapCdInspectTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "snapcd-inspect-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
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

    private string WriteStub(string script)
    {
        var path = Path.Combine(_root, "stub.sh");
        File.WriteAllText(path, "#!/usr/bin/env bash\n" + script);
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }

    [Fact]
    public void Parses_Closures_From_Binary_Output()
    {
        var stub = WriteStub("""
echo '{"closures":[{"rootPath":"modules/app-a","referencedPaths":["shared/naming","shared/network"],"danglingPaths":[]},{"rootPath":"modules/app-b","referencedPaths":[],"danglingPaths":["gone"]}]}'
""");

        var closures = new SnapCdInspect(stub).Discover("/repo", "abc123", new[] { "modules/app-a", "modules/app-b" });

        Assert.Equal(2, closures.Count);
        Assert.Equal(new[] { "shared/naming", "shared/network" }, closures.Single(c => c.RootPath == "modules/app-a").ReferencedPaths);
        Assert.Empty(closures.Single(c => c.RootPath == "modules/app-b").ReferencedPaths);
    }

    [Fact]
    public void Nonzero_Exit_Throws()
    {
        var stub = WriteStub("echo boom >&2\nexit 1\n");

        var ex = Assert.Throws<Exception>(() => new SnapCdInspect(stub).Discover("/repo", "abc123", new[] { "x" }));

        Assert.Contains("boom", ex.Message);
    }

    [Fact]
    public void Embedded_Binary_Extracts_And_Discovers()
    {
        // Only meaningful when the build embedded the binary (CI, or a local build after
        // `go build -o SnapCd.Runner/EmbeddedTools/snapcd-inspect ./tools/snapcd-inspect`).
        using var resource = typeof(SnapCdInspect).Assembly
            .GetManifestResourceStream("SnapCd.Runner.EmbeddedTools.snapcd-inspect");
        if (resource == null || !OperatingSystem.IsLinux()) return;

        var workingDir = Path.Combine(_root, "wd");
        var inspect = new SnapCdInspect(
            Options.Create(new SourceCacheSettings()),
            Options.Create(new WorkingDirectorySettings { WorkingDirectory = workingDir }),
            NullLogger<SnapCdInspect>.Instance);

        var repoDir = Path.Combine(_root, "repo");
        Directory.CreateDirectory(Path.Combine(repoDir, "app"));
        Directory.CreateDirectory(Path.Combine(repoDir, "shared"));
        File.WriteAllText(Path.Combine(repoDir, "app", "main.tf"), "module \"s\" {\n  source = \"../shared\"\n}\n");
        File.WriteAllText(Path.Combine(repoDir, "shared", "main.tf"), "output \"x\" { value = 1 }\n");
        RunGit(repoDir, "init", "-q", "-b", "main");
        RunGit(repoDir, "config", "user.name", "Test");
        RunGit(repoDir, "config", "user.email", "test@example.com");
        RunGit(repoDir, "add", "-A");
        RunGit(repoDir, "commit", "-q", "-m", "fixture");
        var head = RunGit(repoDir, "rev-parse", "HEAD").Trim();

        var closures = inspect.Discover(repoDir, head, new[] { "app" });

        Assert.True(File.Exists(Path.Combine(workingDir, "tools", "snapcd-inspect")));
        Assert.Equal(new[] { "shared" }, closures.Single(c => c.RootPath == "app").ReferencedPaths);
    }

    private static string RunGit(string workingDirectory, params string[] arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = new System.Diagnostics.Process();
        process.StartInfo = startInfo;
        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"git {string.Join(' ', arguments)} failed: {stderr}");

        return stdout;
    }

    [Fact]
    public void Passes_Repo_Commit_And_Roots_As_Arguments()
    {
        var stub = WriteStub("""
echo "{\"closures\":[{\"rootPath\":\"$6\",\"referencedPaths\":[\"$2|$4\"],\"danglingPaths\":[]}]}"
""");

        var closures = new SnapCdInspect(stub).Discover("/some/repo", "sha-1", new[] { "a", "b" });

        Assert.Equal("a,b", closures.Single().RootPath);
        Assert.Equal("/some/repo|sha-1", closures.Single().ReferencedPaths.Single());
    }
}
