// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Cryptography;
using System.Text;
using SnapCd.Contracts;
using SnapCd.Server.Core.Dtos;

namespace SnapCd.Server.Core.Services.RunInputs;

/// <summary>
/// What a run was given, as one value, so a result computed from it can be judged still current.
///
/// A secret contributes the version the vault holds rather than its value: the composition and the
/// input names are both knowable, so a digest over a short secret can be found by guessing.
/// </summary>
public static class RunInputHash
{
    /// <summary>The value a secret-sourced input contributes when its vault version is unknown.</summary>
    public const string UnknownSecretVersion = "";

    public static string Compose(IEnumerable<ModuleResolvedInput> inputs)
    {
        var preimage = string.Join('\n', inputs
            .OrderBy(i => i.Name, StringComparer.Ordinal)
            .Select(i => $"{i.Name}\0{i.Source}\0{Contribution(i)}"));

        return Digest(preimage);
    }

    public static string Digest(string preimage) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(preimage))).ToLowerInvariant();

    /// <summary>
    /// A secret stands for itself by version, so the digest carries no secret material. Everything
    /// else contributes its value, which is already readable wherever the digest is.
    /// </summary>
    private static string Contribution(ModuleResolvedInput input) =>
        IsSecretSourced(input.Source)
            ? input.SecretVersion ?? UnknownSecretVersion
            : input.ResolvedValue;

    private static bool IsSecretSourced(ModuleInputSource source) =>
        source is ModuleInputSource.ModuleSecret;
}
