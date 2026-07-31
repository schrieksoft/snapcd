// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Generators;

// Regenerates the MCP surface by reflecting over the compiled SnapCd.Server.Core assembly (a project
// reference of this generator) plus a syntax-only parse of the controller sources for the literal
// Service.X(...) call each action makes.
//
//   --out       output directory for generated .cs files. Defaults to SnapCd.Server.Core/AI/Mcp/Generated.
//   --check     don't write — exit non-zero if any generated file would differ from the on-disk copy.
internal static class McpCommand
{
    public static Task<int> Run(string[] args, string snapcdRoot)
    {
        var (outDir, checkOnly) = ParseArgs(args, snapcdRoot);

        var assembly = typeof(SnapCd.Server.Core.Database.SnapCdDbContext).Assembly;
        var syntaxIndex = ControllerSyntaxIndex.Build(Path.Combine(snapcdRoot, "SnapCd.Server.Core", "Controllers"));

        Directory.CreateDirectory(outDir);

        var generated = McpSurfaceEmitter.Emit(assembly, syntaxIndex).ToList();
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
                Console.Error.WriteLine("  dotnet run --project generators/SnapCd.Generators -- mcp");
                return Task.FromResult(1);
            }
            Console.WriteLine($"OK — {generated.Count} generated file(s) match on-disk copies.");
            return Task.FromResult(0);
        }

        Console.WriteLine($"Done — {generated.Count} generated, {written} written, {existing.Count} stale removed.");
        return Task.FromResult(0);
    }

    private static (string outDir, bool checkOnly) ParseArgs(string[] args, string snapcdRoot)
    {
        string? outDir = null;
        var checkOnly = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
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

        outDir ??= Path.Combine(snapcdRoot, "SnapCd.Server.Core", "AI", "Mcp", "Generated");
        return (outDir, checkOnly);
    }
}
