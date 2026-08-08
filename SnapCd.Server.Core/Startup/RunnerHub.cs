// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using SnapCd.Server.Core.Hubs.Filters;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.RunnerConnectionValidator;

namespace SnapCd.Server.Core.Startup;

public static class RunnerHubExtensions
{
    public static IServiceCollection AddSnapCdRunnerHub(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            // Increase from default 32KB to handle large plan data with many resources
            options.MaximumReceiveMessageSize =  1024 * 1024; // 1MB
            options.StreamBufferCapacity = 20;
            options.EnableDetailedErrors = true; // Help diagnose connection issues

            // SignalR resolves filters from this list, not from IHubFilter registrations alone.
            options.AddFilter<TokenValidationFilter>();
            options.AddFilter<Services.CallerContext.CallerContextHubFilter>();


        });
        services.AddSingleton<IHubFilter, TokenValidationFilter>();
        services.AddSingleton<IHubFilter, Services.CallerContext.CallerContextHubFilter>();
        services.AddScoped<RunnerConnectionValidator>();
        services.AddScoped<RunnerJobAuthorizationService>();


        return services;
    }
}

