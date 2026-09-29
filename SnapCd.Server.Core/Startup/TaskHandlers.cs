// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Hubs.Handlers;
using SnapCd.Server.Core.Hubs.Handlers.SplitMigrate;

namespace SnapCd.Server.Core.Startup;

public static class TaskHandlers
{
    public static IServiceCollection AddSnapCdTaskHandlers(this IServiceCollection services)
    {
        services.AddScoped<ApplyGetDefinitiveRevisionHandler>();
        services.AddScoped<DestroyGetDefinitiveRevisionHandler>();
        services.AddScoped<ApplyGetModuleHandler>();
        services.AddScoped<DestroyGetModuleHandler>();
        services.AddScoped<ApplyInitHandler>();
        services.AddScoped<DestroyInitHandler>();
        services.AddScoped<ApplyValidateHandler>();
        services.AddScoped<DestroyValidateHandler>();
        services.AddScoped<ApplyPolicyValidateHandler>();
        services.AddScoped<DestroyPolicyValidateHandler>();
        services.AddScoped<ApplyVariablesHandler>();
        services.AddScoped<DestroyVariablesHandler>();
        services.AddScoped<PlanHandler>();
        services.AddScoped<SplitGetModuleHandler>();
        services.AddScoped<SplitInitHandler>();
        services.AddScoped<SplitValidateHandler>();
        services.AddScoped<SplitPlanHandler>();
        services.AddScoped<SplitPlanEmptyVerifyHandler>();
        services.AddScoped<SplitRefactorValidateHandler>();
        services.AddScoped<SplitRefactorDiffHandler>();
        services.AddScoped<SplitMigrateMapHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.Transfers.TransferStepHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.StateMigrations.StateListFilteredGetModuleHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.StateMigrations.StateListFilteredInitHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.StateMigrations.MoveGetModuleHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.StateMigrations.MoveInitHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.StateMigrations.ImportGetModuleHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.StateMigrations.ImportInitHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.StateMigrations.RemoveGetModuleHandler>();
        services.AddScoped<SnapCd.Server.Core.Hubs.Handlers.StateMigrations.RemoveInitHandler>();
        services.AddScoped<SplitMigrateProveHandler>();
        services.AddScoped<SplitMigrateRunHandler>();
        services.AddScoped<SplitMigrateVerifyHandler>();
        services.AddScoped<PlanDestroyHandler>();
        services.AddScoped<ApplyFromPlanHandler>();
        services.AddScoped<DestroyFromPlanHandler>();
        services.AddScoped<ApplyOutputHandler>();
        services.AddScoped<DestroyOutputHandler>();
        services.AddScoped<SourceRefreshHandler>();
        services.AddScoped<ReportRunningTaskHandler>();
        services.AddScoped<CancelKillHandler>();

        return services;
    }
}

