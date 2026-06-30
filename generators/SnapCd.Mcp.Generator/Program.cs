// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using SnapCd.Mcp.Generator;

// CLI: dotnet run --project SnapCd.Mcp.Generator [--project <csproj>] [--out <dir>] [--check]
//
//   --project   csproj to load and codegen against. Defaults to ../SnapCd.Server.Core/SnapCd.Server.Core.csproj
//               relative to this tool's location.
//   --out       output directory for generated .cs files. Defaults to <project-dir>/Mcp/Generated.
//   --check     don't write — exit non-zero if any generated file would differ from the on-disk copy.
//               Use this in CI / pre-commit to catch drift.

var (projectPath, outDir, checkOnly) = ParseArgs(args);

MSBuildLocator.RegisterDefaults();

using var workspace = MSBuildWorkspace.Create();
workspace.RegisterWorkspaceFailedHandler(e =>
{
    if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
        Console.Error.WriteLine($"workspace failure: {e.Diagnostic.Message}");
});

Console.WriteLine($"Loading {projectPath}…");
var project = await workspace.OpenProjectAsync(projectPath);
var compilation = await project.GetCompilationAsync()
    ?? throw new InvalidOperationException("Failed to get compilation from project.");

Directory.CreateDirectory(outDir);

var generated = McpSurfaceEmitter.Emit(compilation).ToList();
var existing = Directory.GetFiles(outDir, "*McpSurface*.g.cs", SearchOption.TopDirectoryOnly)
    .ToDictionary(p => Path.GetFileName(p)!, p => p);

var drift = false;
var written = 0;
foreach (var file in generated)
{
    var path = Path.Combine(outDir, file.FileName);
    var onDisk = File.Exists(path) ? File.ReadAllText(path) : null;
    var changed = !string.Equals(onDisk, file.Source, StringComparison.Ordinal);

    if (changed)
    {
        if (checkOnly)
        {
            Console.Error.WriteLine($"DRIFT: {file.FileName} would change");
            drift = true;
        }
        else
        {
            File.WriteAllText(path, file.Source);
            Console.WriteLine($"  wrote {file.FileName}");
            written++;
        }
    }
    existing.Remove(file.FileName);
}

// Files no longer produced by any controller are stale — clean them up so the folder always
// matches the current annotated surface.
foreach (var stale in existing)
{
    if (checkOnly)
    {
        Console.Error.WriteLine($"DRIFT: {stale.Key} is stale (no longer produced)");
        drift = true;
    }
    else
    {
        File.Delete(stale.Value);
        Console.WriteLine($"  removed {stale.Key}");
    }
}

if (checkOnly)
{
    if (drift)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Generated MCP surface is out of date. Run:");
        Console.Error.WriteLine("  dotnet run --project SnapCd.Mcp.Generator");
        return 1;
    }
    Console.WriteLine($"OK — {generated.Count} generated file(s) match on-disk copies.");
    return 0;
}

Console.WriteLine($"Done — {generated.Count} generated, {written} written, {existing.Count} stale removed.");
return 0;

static (string projectPath, string outDir, bool checkOnly) ParseArgs(string[] args)
{
    string? projectPath = null;
    string? outDir = null;
    var checkOnly = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--project" when i + 1 < args.Length:
                projectPath = Path.GetFullPath(args[++i]);
                break;
            case "--out" when i + 1 < args.Length:
                outDir = Path.GetFullPath(args[++i]);
                break;
            case "--check":
                checkOnly = true;
                break;
            default:
                Console.Error.WriteLine($"unknown arg: {args[i]}");
                Environment.Exit(2);
                break;
        }
    }

    // Default: assume the tool lives at applications/snapcd/generators/SnapCd.Mcp.Generator/.
    // AppContext.BaseDirectory is the build output: bin/<Config>/net10.0/. Walk up 5 levels
    // (net10.0 → bin/<Config> → bin → project dir → generators → snapcd) to reach applications/snapcd/.
    var snapcdRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    projectPath ??= Path.Combine(snapcdRoot, "SnapCd.Server.Core", "SnapCd.Server.Core.csproj");
    outDir ??= Path.Combine(Path.GetDirectoryName(projectPath)!, "AI", "Mcp", "Generated");

    if (!File.Exists(projectPath))
        throw new FileNotFoundException($"Project not found: {projectPath}");

    return (projectPath, outDir, checkOnly);
}
