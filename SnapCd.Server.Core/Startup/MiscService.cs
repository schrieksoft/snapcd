// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Authorization;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.Dashboard;
using SnapCd.Server.Core.Services.DataSeeder;
using SnapCd.Server.Core.Services.DependencyGraph;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Core.Services.Notification;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration;
using SnapCd.Server.Core.Services.ViewManagement;
using RunnerSelectionService = SnapCd.Server.Core.Services.RunnerSelectionService;

namespace SnapCd.Server.Core.Startup;

public static class MiscService
{
    public static IServiceCollection AddSnapCdMiscServices(this IServiceCollection services, ConfigurationManager configuration, bool isDevelopment)
    {
        // Runner Selection
        services.AddScoped<RunnerSelectionService>();

        // Log redactor — singleton, shared by REST logs endpoint (Phase 4) + MCP
        // redacted-logs Resource (Phase 6). Single chokepoint, identical scrub policy.
        services.AddSingleton<SnapCd.Server.Core.Logging.ILogRedactor, SnapCd.Server.Core.Logging.DefaultLogRedactor>();

        services.AddScoped<SnapCd.Server.Core.Services.AgentConnectionValidator.AgentConnectionValidator>();

        services.AddScoped<ResolvedConfigurationService>();
        services.AddScoped<DependencyGraphService>();

        // Parameter Resolution (for Terraform)
        services.AddScoped<ParamResolverFactory>();

        // Database View Management
        services.AddScoped<IViewManager, ViewManager>();

        // Job Execution Services
        services.AddScoped<JobService>();
        services.AddScoped<SecuredJobService>();
        services.AddScoped<JobOrchestrationService>();
        services.AddScoped<SourceChangedService>();

        // Authentication and Security Services
        services.AddScoped<AccessTokenService>();
        services.AddScoped<EmailConfirmationService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<OrganizationService>();

        // Notification Services (Singletons)
        services.AddSingleton<JobCreatedNotificationService>();
        services.AddSingleton<JobUpdatedNotificationService>();
        services.AddSingleton<LogReceivedNotificationService>();
        services.AddSingleton<RunnerAvailabilityModifiedNotificationService>();
        services.AddSingleton<AgentAvailabilityModifiedNotificationService>();
        services.AddSingleton<MissionRunModifiedNotificationService>();
        services.AddSingleton<ModuleSagaModifiedNotificationService>();
        services.AddSingleton<ModuleStateModifiedNotificationService>();
        services.AddSingleton<ModuleJobApprovalModifiedNotificationService>();
        services.AddSingleton<ModuleApprovalThresholdModifiedNotificationService>();
        services.AddSingleton<LicenseUsageModifiedNotificationService>();


        //services.AddScoped<ModuleJobApprovalRepository>(); // currently doesn't exist

        services.AddScoped<CustomOutputMapper>();


        // Execution Services
        services.AddScoped<SourceRefreshJob>();

        services.AddScoped<LogService>();

        // Other services
        services.AddScoped<IPrincipalProvider, HttpContextPrincipalProvider>();
        services.AddScoped<HttpContextPrincipalProvider>();
        services.AddScoped<SnapCdUserManager>();
        
        if (System.Diagnostics.Debugger.IsAttached || configuration.GetValue<bool>("UseDebugDataSeeder"))
        {
            services.AddScoped<IDataSeeder, DebugDataSeeder>();
        }
        else
        {
            services.AddScoped<IDataSeeder, ProductionDataSeeder>();
        }


        // Secret Migrator
        services.AddScoped<Services.SecretMigrator.SecretMigratorService>();

        services.AddScoped<IQuotaGatingService, QuotaGatingService>();
        services.AddScoped<QuotaService>();
        services.AddScoped<QuotaEnforcementService>();

        // Register HttpClient for license verification
        services.AddHttpClient();

        // Identity Services
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<PostAuthRedirectResolver>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        // Version service (singleton since version doesn't change during runtime)
        services.AddSingleton<IVersionService, VersionService>();


        services.AddScoped<MemberService>();
        services.AddScoped<MemberServiceFactory>();
        // /Account
        services.AddScoped<UserLoginService>();


        services.AddScoped<InvitationCleanupJob>();

        // No-op quota usage defaults (self-hosted). SaaS calls AddSaaSQuotaUsageServices()
        // which Replace()s these with the real, DB-backed implementations.
        services.AddScoped<Services.QuotaUsage.IQuotaUsageForInvitationService, Services.QuotaUsage.NoOpQuotaUsageForInvitationService>();
        services.AddScoped<Services.QuotaUsage.IQuotaUsageForEmailConfirmationService, Services.QuotaUsage.NoOpQuotaUsageForEmailConfirmationService>();
        services.AddScoped<Services.QuotaUsage.IQuotaUsageForPasswordResetService, Services.QuotaUsage.NoOpQuotaUsageForPasswordResetService>();
        services.AddScoped<IQuotaUsageForInvitationServiceFactory, NoOpQuotaUsageForInvitationServiceFactory>();
        services.AddScoped<IQuotaUsageForEmailConfirmationServiceFactory, NoOpQuotaUsageForEmailConfirmationServiceFactory>();
        services.AddSingleton<IQuotaUsageForPasswordResetServiceFactory, NoOpQuotaUsageForPasswordResetServiceFactory>();

        // No-op terms acceptance (self-hosted). SaaS Replace()s with the real DB-backed implementation.
        services.AddScoped<ITermsAcceptanceService, NoOpTermsAcceptanceService>();

        services.AddScoped<OrphanedJobCleanupService>();

        return services;
    }
}