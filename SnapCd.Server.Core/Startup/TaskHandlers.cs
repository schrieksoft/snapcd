// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Hubs.Handlers;

namespace SnapCd.Server.Core.Startup;

public static class TaskHandlers
{
    public static IServiceCollection AddSnapCdTaskHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetDefinitiveRevisionHandler>();
        services.AddScoped<GetModuleHandler>();
        services.AddScoped<InitHandler>();
        services.AddScoped<ValidateHandler>();
        services.AddScoped<PolicyValidateHandler>();
        services.AddScoped<VariableHandler>();
        services.AddScoped<PlanHandler>();
        services.AddScoped<PlanDestroyHandler>();
        services.AddScoped<ApplyFromPlanHandler>();
        services.AddScoped<DestroyFromPlanHandler>();
        services.AddScoped<OutputHandler>();
        services.AddScoped<SourceRefreshHandler>();
        services.AddScoped<ReportRunningTaskHandler>();
        services.AddScoped<CancelKillHandler>();
        services.AddScoped<CancelGracefulHandler>();

        return services;
    }
}

