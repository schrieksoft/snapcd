// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Cryptography;
using System.Text;

namespace SnapCd.Cli.Auth;

/// <summary>A PKCE verifier and its S256 challenge (RFC 7636).</summary>
public sealed record Pkce(string Verifier, string Challenge)
{
    public static Pkce Create()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(48));
        return new Pkce(verifier, ChallengeFor(verifier));
    }

    public static string ChallengeFor(string verifier) =>
        Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    public static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
