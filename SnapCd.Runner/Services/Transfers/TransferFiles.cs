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

    /// <summary>Where demonolith keeps the pulled state, the fragment and the output values.</summary>
    public const string WorkDirectory = ".demono-transfer";

    /// <summary>A transfer has one receiver, so none of its files is named for whose it is.</summary>
    public const string FragmentStateFile = "fragment.tfstate";
    public const string FragmentMetaFile = "fragment.yaml";
    public const string OutputsFile = "outputs.yaml";

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


    /// <summary>
    /// The output values this Module's prove produced, which the other half's plan reads. A
    /// transfer threads one producer's values, so there is one file.
    /// </summary>
    public static async Task<string?> ReadOutputs(string? rootDirectory)
    {
        var path = Path.Combine(WorkDir(rootDirectory), OutputsFile);

        return File.Exists(path) ? await File.ReadAllTextAsync(path) : null;
    }

    /// <summary>The fragment a source's map cut, and the meta that pins it.</summary>
    public static async Task<(string? Fragment, string? Meta)> ReadFragment(string? rootDirectory)
    {
        var work = WorkDir(rootDirectory);
        if (!Directory.Exists(work)) return (null, null);

        var state = Path.Combine(work, FragmentStateFile);
        var meta = Path.Combine(work, FragmentMetaFile);

        if (!File.Exists(state) || !File.Exists(meta)) return (null, null);

        return (await File.ReadAllTextAsync(state), await File.ReadAllTextAsync(meta));
    }

    /// <summary>Puts the source's fragment where the receiver's map will look for it.</summary>
    public static async Task WriteFragment(string? rootDirectory, string fragment, string meta)
    {
        var work = WorkDir(rootDirectory);
        Directory.CreateDirectory(work);

        await File.WriteAllTextAsync(Path.Combine(work, FragmentStateFile), fragment);
        await File.WriteAllTextAsync(Path.Combine(work, FragmentMetaFile), meta);
    }

    /// <summary>The other half's output values, for this root's prove to thread.</summary>
    public static async Task WriteOutputs(string? rootDirectory, string outputs)
    {
        var work = WorkDir(rootDirectory);
        Directory.CreateDirectory(work);
        await File.WriteAllTextAsync(Path.Combine(work, OutputsFile), outputs);
    }

    private static string Root(string? rootDirectory) =>
        string.IsNullOrWhiteSpace(rootDirectory) ? "." : rootDirectory;

    private static string WorkDir(string? rootDirectory) =>
        Path.Combine(Root(rootDirectory), WorkDirectory);
}
