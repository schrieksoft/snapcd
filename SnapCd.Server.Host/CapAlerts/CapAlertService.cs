// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Telemetry;
using SnapCd.Server.Host.Installations;
using SnapCd.Server.Host.Telemetry;

namespace SnapCd.Server.Host.CapAlerts;

/// <summary>An organization near, at or over its module cap, with the text to show.</summary>
public record CapAlert(Guid OrganizationId, WhatsNewKind Kind, int Cap, int Count, string Title, string Markdown);

/// <summary>Decides locally which of the three cap texts an organization gets, if any. Near is 90% of the cap.</summary>
public class CapAlertService(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    ILicenseInfoProvider licenseInfoProvider,
    InstallationService installation)
{
    /// <summary>Used until the beacon has delivered texts, and whenever it never does.</summary>
    public static readonly IReadOnlyDictionary<WhatsNewKind, WhatsNewEntryDto> Defaults = new Dictionary<WhatsNewKind, WhatsNewEntryDto>
    {
        [WhatsNewKind.CapNear] = Default(WhatsNewKind.CapNear, "Approaching the module limit", "This organization has {count} of {cap} modules. A larger edition raises the limit; see the License page."),
        [WhatsNewKind.CapAt] = Default(WhatsNewKind.CapAt, "Module limit reached", "This organization has all {cap} modules its edition allows. Adding another needs a larger edition; see the License page."),
        [WhatsNewKind.CapOver] = Default(WhatsNewKind.CapOver, "Over the module limit", "This organization has {count} modules but its edition allows {cap}. Existing modules keep working; new ones cannot be added until the licence covers them. See the License page."),
    };

    private static WhatsNewEntryDto Default(WhatsNewKind kind, string title, string markdown) =>
        new(Guid.Empty, DateTime.MinValue, kind.ToString(), title, markdown);

    public async Task<CapAlert?> EvaluateAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty) return null;

        var license = await licenseInfoProvider.GetLicenseInfoAsync(organizationId);
        if (license.MaxModules is not { } cap || cap <= 0) return null;

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var count = await db.Modules.CountAsync(m => m.OrganizationId == organizationId, ct);

        var kind = Classify(count, cap);
        if (kind is null) return null;

        var text = await TextAsync(kind.Value, ct);
        return new CapAlert(organizationId, kind.Value, cap, count, Fill(text.Title, count, cap), Fill(text.Markdown, count, cap));
    }

    public static WhatsNewKind? Classify(int count, int cap) =>
        count > cap ? WhatsNewKind.CapOver
        : count == cap ? WhatsNewKind.CapAt
        : count * 10 >= cap * 9 ? WhatsNewKind.CapNear
        : null;

    private async Task<WhatsNewEntryDto> TextAsync(WhatsNewKind kind, CancellationToken ct)
    {
        var row = await installation.GetAsync(ct);
        return TelemetrySnapshotProvider.ReadStored(row.WhatsNewJson).FirstOrDefault(e => e.Kind == kind.ToString())
               ?? Defaults[kind];
    }

    private static string Fill(string template, int count, int cap) =>
        template.Replace("{count}", count.ToString()).Replace("{cap}", cap.ToString());
}
