// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json;

namespace SnapCd.Runner.Services.Transfers;

/// <summary>
/// The files one half of a transfer reads and writes in the checkout. Every one of them crosses between
/// the two Modules through the server, because the two runners never see each other.
/// </summary>
public static class TransferFiles
{
    /// <summary>The transfer map, which every step loads to know what is moving.</summary>
    public const string MapFile = "demonolith-transfer-map.yaml";

    /// <summary>Where demonolith keeps the pulled state, the fragments and the output values.</summary>
    public const string WorkDirectory = ".demono-transfer";

    /// <summary>
    /// Writes the map into the root before a step runs. The map is the transfer's identity, so it
    /// is written exactly as given: a re-serialised copy would hash differently.
    /// </summary>
    public static async Task WriteMap(string? rootDirectory, string map)
    {
        var root = Root(rootDirectory);
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, MapFile), map);
    }

    /// <summary>The root's own directory name, which is the token demonolith names artefacts by.</summary>
    private static string BaseName(string? rootDirectory) =>
        Path.GetFileName(Path.TrimEndingDirectorySeparator(Root(rootDirectory)));

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

    /// <summary>
    /// The fragment a source's map cut for its receiver, named for the receiver rather than for
    /// this root, so the name comes from the map rather than from the directory.
    /// </summary>
    public static async Task<(string? Fragment, string? Meta)> ReadFragmentFor(
        string? rootDirectory, string receiverBase)
    {
        var work = WorkDir(rootDirectory);
        if (!Directory.Exists(work)) return (null, null);

        var state = Path.Combine(work, $"fragment-{receiverBase}.tfstate");
        var meta = Path.Combine(work, $"fragment-{receiverBase}.yaml");

        if (!File.Exists(state) || !File.Exists(meta)) return (null, null);

        return (await File.ReadAllTextAsync(state), await File.ReadAllTextAsync(meta));
    }

    /// <summary>
    /// Puts the source's fragment where a receiver's map will look for it. Named for this root,
    /// which is the receiver demonolith addressed the fragment to.
    /// </summary>
    public static async Task WriteFragment(string? rootDirectory, string fragment, string meta)
    {
        var work = WorkDir(rootDirectory);
        Directory.CreateDirectory(work);

        var baseName = BaseName(rootDirectory);
        await File.WriteAllTextAsync(Path.Combine(work, $"fragment-{baseName}.tfstate"), fragment);
        await File.WriteAllTextAsync(Path.Combine(work, $"fragment-{baseName}.yaml"), meta);
    }

    /// <summary>
    /// The producer's output files, for a consumer's prove. demonolith looks them up by producer
    /// name, so each keeps the name it was written under rather than being merged.
    /// </summary>
    public static async Task WriteOutputs(string? rootDirectory, string outputsJson)
    {
        var files = JsonSerializer.Deserialize<Dictionary<string, string>>(outputsJson);
        if (files is null || files.Count == 0) return;

        var work = WorkDir(rootDirectory);
        Directory.CreateDirectory(work);

        foreach (var (name, content) in files)
            await File.WriteAllTextAsync(Path.Combine(work, Path.GetFileName(name)), content);
    }

    private static string Root(string? rootDirectory) =>
        string.IsNullOrWhiteSpace(rootDirectory) ? "." : rootDirectory;

    private static string WorkDir(string? rootDirectory) =>
        Path.Combine(Root(rootDirectory), WorkDirectory);
}
