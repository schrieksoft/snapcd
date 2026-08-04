// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.AspNetCore.Components;
using SnapCd.Server.Core.Filters;
using RouteAttribute = Microsoft.AspNetCore.Components.RouteAttribute;

namespace SnapCd.Server.Core.Tests.Tests.Permissions;

/// <summary>
/// Product surface must be tagged [OrganizationScopedFeature]: that attribute is what the
/// activation gate (layouts) and the API filter read to decide whether an organization is
/// entitled to reach it. An untagged product page is silently reachable by URL even when the
/// nav link is hidden — which is exactly how /Dashboard and /ApiReference were missed.
///
/// Account, billing and onboarding surfaces are deliberately exempt: a user must be able to
/// reach the screens that activate the organization in the first place. This test covers the
/// Core assembly; edition-specific pages (billing, onboarding) live in their own assemblies and
/// are ungated by design.
/// </summary>
public class OrganizationScopedFeatureCoverageTests
{
    /// <summary>
    /// Routes that must stay reachable without an activated organization. Add to this list only
    /// with a reason — everything here is, by construction, ungated.
    /// </summary>
    private static readonly HashSet<string> ExemptRoutePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Account",         // sign-in, registration, profile, organization selection
        "/Organizations",   // pick or create an organization
        "/Dashboard/Error", // error pages must render in every state
        "/Error"
    };

    [Fact]
    public void EveryRoutableProductPageIsTaggedAsOrganizationScoped()
    {
        var failures = new List<string>();

        foreach (var pageType in RoutablePages())
        {
            var routes = pageType.GetCustomAttributes<RouteAttribute>()
                .Select(r => r.Template)
                .ToList();

            if (routes.Count == 0) continue;
            if (routes.All(IsExempt)) continue;

            var tagged = pageType.GetCustomAttributes(typeof(OrganizationScopedFeatureAttribute), inherit: true).Any()
                         || pageType.GetCustomAttributes(typeof(OrganizationScopedIAMAttribute), inherit: true).Any();

            if (!tagged)
                failures.Add($"{pageType.FullName} ({string.Join(", ", routes)})");
        }

        Assert.True(failures.Count == 0,
            "These routable pages are neither tagged [OrganizationScopedFeature]/[OrganizationScopedIAM] nor listed as " +
            $"exempt, so they are reachable without an activated organization:{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// The exemption list must not rot: an entry naming a route that no longer exists hides the
    /// fact that the surface it protected has moved.
    /// </summary>
    [Fact]
    public void ExemptionListHasNoStaleEntries()
    {
        var allRoutes = RoutablePages()
            .SelectMany(p => p.GetCustomAttributes<RouteAttribute>())
            .Select(r => r.Template)
            .ToList();

        var stale = ExemptRoutePrefixes
            .Where(prefix => !allRoutes.Any(route => IsUnder(route, prefix)))
            .ToList();

        Assert.True(stale.Count == 0,
            $"Exempt route prefixes matching no page: {string.Join(", ", stale)}");
    }

    private static bool IsExempt(string route) => ExemptRoutePrefixes.Any(prefix => IsUnder(route, prefix));

    private static bool IsUnder(string route, string prefix)
    {
        var normalized = route.StartsWith('/') ? route : "/" + route;
        return normalized.Equals(prefix, StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<Type> RoutablePages() =>
        typeof(SnapCd.Server.Core.Marker).Assembly
            .GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttributes<RouteAttribute>().Any());
}
