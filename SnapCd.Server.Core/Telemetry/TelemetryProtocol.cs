// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json.Serialization;

namespace SnapCd.Server.Core.Telemetry;

/// <summary>Body of the daily usage beacon. Counts and version only; nothing here can identify an operator. <paramref name="WhatsNewSinceUtc"/> is the newest feed entry the install already holds, so a normal day returns nothing.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record TelemetryReportRequest(Guid SeedGuid, string Version, int ModuleCount, long JobsTotal, DateTime? WhatsNewSinceUtc = null);

/// <summary>Response to the beacon: the newest release, and the feed the What's New page shows.</summary>
public record TelemetryReportResponse(string? LatestVersion, List<WhatsNewEntryDto> WhatsNew);

/// <summary>One What's New entry as delivered. <paramref name="Markdown"/> is rendered through <see cref="WhatsNewMarkdown"/>.</summary>
public record WhatsNewEntryDto(Guid Id, DateTime PublishedUtc, string Title, string Markdown);
