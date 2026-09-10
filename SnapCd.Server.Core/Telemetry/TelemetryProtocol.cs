// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json.Serialization;

namespace SnapCd.Server.Core.Telemetry;

/// <summary>Body of the daily usage beacon. Counts and version only; nothing here can identify an operator.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record TelemetryReportRequest(Guid SeedGuid, string Version, int ModuleCount, long JobsTotal);

/// <summary>Response to the beacon: the three cap-alert texts filled for the install's edition, plus the newest published notices and alerts, the whole set every time.</summary>
public record TelemetryReportResponse(List<WhatsNewEntryDto> WhatsNew);

/// <summary>One What's New entry as delivered. <paramref name="Kind"/> is a <see cref="WhatsNewKind"/> name; <paramref name="Markdown"/> is rendered through <see cref="WhatsNewMarkdown"/>. Cap texts still carry {count}, which the install fills at render time.</summary>
public record WhatsNewEntryDto(Guid Id, DateTime PublishedUtc, string Kind, string Title, string Markdown);

/// <summary>Notices are news, alerts need attention, and the three cap kinds show only when an organization's module count is near, at, or over its cap.</summary>
public enum WhatsNewKind
{
    Notice,
    Alert,
    CapNear,
    CapAt,
    CapOver
}

public static class WhatsNewKinds
{
    public static bool IsCap(string kind) => kind is nameof(WhatsNewKind.CapNear) or nameof(WhatsNewKind.CapAt) or nameof(WhatsNewKind.CapOver);
    public static bool IsCap(WhatsNewKind kind) => kind is WhatsNewKind.CapNear or WhatsNewKind.CapAt or WhatsNewKind.CapOver;
}
