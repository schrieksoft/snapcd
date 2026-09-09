// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Licensing.Protocol;

/// <summary>Body of <c>POST api/licenses/issue</c>. <paramref name="SeedGuid"/> is null from servers that predate installation seeds.</summary>
public record LicenseIssueRequest(string LicenseKey, Guid? SeedGuid = null);

/// <summary>Body of <c>POST api/licenses/refresh</c>.</summary>
public record LicenseRefreshRequest(string LicenseKey, string? CurrentToken, Guid? SeedGuid = null);

/// <summary>Response to both licence calls.</summary>
public record LicenseTokenResponse(string Token, DateTime ExpiresAtUtc, DateTime LicensePeriodEndUtc);
