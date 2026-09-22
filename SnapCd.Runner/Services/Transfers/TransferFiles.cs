// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Runner.Services.Transfers;

/// <summary>
/// The files a transfer slice reads and writes in the checkout. Every one of them crosses between
/// the two Modules through the server, because the two runners never see each other.
/// </summary>
public static class TransferFiles
{
    /// <summary>The transfer map, which every slice loads to know what is moving.</summary>
    public const string MapFile = "demonolith-transfer-map.yaml";

    /// <summary>Where demonolith keeps the pulled state, the fragments and the output values.</summary>
    public const string WorkDirectory = ".demono-transfer";

    /// <summary>
    /// Writes the map into the root before a slice runs. The map is the transfer's identity, so it
    /// is written exactly as given: a re-serialised copy would hash differently.
    /// </summary>
    public static async Task WriteMap(string? rootDirectory, string map)
    {
        var root = Root(rootDirectory);
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, MapFile), map);
    }

    /// <summary>Puts the fragment the other Module produced where this Module's slice looks for it.</summary>
    public static async Task WriteFragment(string? rootDirectory, string baseName, string? state, string? meta)
    {
        if (state == null) return;

        var work = WorkDir(rootDirectory);
        Directory.CreateDirectory(work);

        await File.WriteAllTextAsync(Path.Combine(work, $"fragment-{baseName}.tfstate"), state);

        if (meta != null)
            await File.WriteAllTextAsync(Path.Combine(work, $"fragment-{baseName}.yaml"), meta);
    }

    /// <summary>The fragment this Module's slice wrote, for the other one.</summary>
    public static async Task<(string? State, string? Meta)> ReadFragment(string? rootDirectory, string baseName)
    {
        var work = WorkDir(rootDirectory);
        var state = Path.Combine(work, $"fragment-{baseName}.tfstate");
        var meta = Path.Combine(work, $"fragment-{baseName}.yaml");

        return (
            File.Exists(state) ? await File.ReadAllTextAsync(state) : null,
            File.Exists(meta) ? await File.ReadAllTextAsync(meta) : null);
    }

    /// <summary>
    /// Puts the values the other Module produced where this Module's prove looks for them. Keyed by
    /// the filename demonolith expects, which the server carries verbatim.
    /// </summary>
    public static async Task WriteOutputs(string? rootDirectory, IReadOnlyDictionary<string, string> outputs)
    {
        if (outputs.Count == 0) return;

        var work = WorkDir(rootDirectory);
        Directory.CreateDirectory(work);

        foreach (var (name, content) in outputs)
            await File.WriteAllTextAsync(Path.Combine(work, name), content);
    }

    /// <summary>
    /// The output files this Module's prove produced, by filename. The server stores them and hands
    /// them to whichever Module consumes them.
    /// </summary>
    public static async Task<Dictionary<string, string>> ReadOutputs(string? rootDirectory)
    {
        var work = WorkDir(rootDirectory);
        var outputs = new Dictionary<string, string>();

        if (!Directory.Exists(work)) return outputs;

        foreach (var path in Directory.EnumerateFiles(work, "outputs-*.yaml"))
            outputs[Path.GetFileName(path)] = await File.ReadAllTextAsync(path);

        return outputs;
    }

    private static string Root(string? rootDirectory) =>
        string.IsNullOrWhiteSpace(rootDirectory) ? "." : rootDirectory;

    private static string WorkDir(string? rootDirectory) =>
        Path.Combine(Root(rootDirectory), WorkDirectory);
}
