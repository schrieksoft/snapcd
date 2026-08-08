// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Hubs.Filters;
using SnapCd.Server.Core.Services.CallerContext;
using SnapCd.Server.Core.Startup;

namespace SnapCd.Server.Core.Tests.Tests.Services;

/// <summary>
/// SignalR builds its filter pipeline from HubOptions.Filters. An IHubFilter present only as a
/// container registration is never invoked, which silently disables it.
/// </summary>
public class HubFilterRegistrationTests
{
    private static IReadOnlyList<object> ConfiguredFilters()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSnapCdRunnerHub();

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions>>().Value;
        var filters = typeof(HubOptions)
            .GetProperty("HubFilters", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .GetValue(options);

        return ((IEnumerable<object>)filters!).ToList();
    }

    // AddFilter<T>() wraps the type in an internal HubFilterFactory rather than storing an instance.
    private static bool Contains<TFilter>(IReadOnlyList<object> filters) =>
        filters.Any(f => f is TFilter || FilterTypeOf(f) == typeof(TFilter));

    private static Type? FilterTypeOf(object filter) =>
        filter.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(field => field.GetValue(filter) as Type)
            .FirstOrDefault(t => t != null);

    [Fact]
    public void CallerContextHubFilter_is_in_the_signalr_filter_pipeline()
    {
        Assert.True(Contains<CallerContextHubFilter>(ConfiguredFilters()));
    }

    [Fact]
    public void TokenValidationFilter_is_in_the_signalr_filter_pipeline()
    {
        Assert.True(Contains<TokenValidationFilter>(ConfiguredFilters()));
    }
}
