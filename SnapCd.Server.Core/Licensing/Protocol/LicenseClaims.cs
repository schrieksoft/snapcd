// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Licensing.Protocol;

/// <summary>Claim names in a licence token. Both the issuer and the validator read from here.</summary>
public static class LicenseClaims
{
    public const string Subject = "sub";
    public const string Tier = "tier";
    public const string TokenId = "jti";
    public const string MaxModules = "max_modules";
    public const string LicensePeriodEnd = "license_period_end";
    public const string Seed = "seed";
}
