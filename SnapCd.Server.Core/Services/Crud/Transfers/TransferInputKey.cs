// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.Services.Crud.Transfers;

/// <summary>
/// The fingerprint of everything a participant's slice result depends on. A proof is only as fresh
/// as its key: the Transfer's verdict is green when every side has a green prove step whose key
/// still matches its current inputs, so a push to either branch turns the verdict amber rather than
/// leaving a stale green in place.
///
/// The key deliberately excludes the backend state's serial and lineage. A proof is invalidated by
/// the same things that trigger an ordinary job - a new commit, a changed parameter, a changed
/// upstream output - not by the state moving underneath it.
/// </summary>
public static class TransferInputKey
{
    /// <summary>
    /// Computes one participant's key. Null parts are included as empty, so a slice that gains a
    /// fragment or a threaded value keys differently from one that never had it.
    /// </summary>
    public static string Compute(
        string? definitiveRevision,
        ResolvedModule? declared,
        string? fragment,
        IReadOnlyDictionary<string, string>? threadedOutputs)
    {
        var parts = new StringBuilder();
        parts.Append(definitiveRevision ?? string.Empty).Append('\n');
        parts.Append(Hash(declared == null ? string.Empty : JsonSerializer.Serialize(declared))).Append('\n');
        parts.Append(Hash(fragment ?? string.Empty)).Append('\n');

        // Ordered by name: the same values threaded in a different order are the same inputs.
        if (threadedOutputs != null)
            foreach (var (name, value) in threadedOutputs.OrderBy(o => o.Key, StringComparer.Ordinal))
                parts.Append(name).Append('=').Append(Hash(value)).Append('\n');

        return Hash(parts.ToString());
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
