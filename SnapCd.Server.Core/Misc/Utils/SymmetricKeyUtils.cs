// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.RegularExpressions;

namespace SnapCd.Server.Core.Misc.Utils;

public static class SymmetricKeyUtils
{
    public static string ExtractBase64FromPem(string pemString)
    {
        // Use regex to extract the Base64 string between BEGIN and END delimiters
        var match = Regex.Match(pemString, @"-----BEGIN SYMMETRIC KEY-----(.*?)-----END SYMMETRIC KEY-----",
            RegexOptions.Singleline);

        if (match.Success)
            // Remove whitespace characters and return the Base64 encoded string
            return match.Groups[1].Value.Trim();
        else
            throw new ArgumentException("Invalid PEM format");
    }
}