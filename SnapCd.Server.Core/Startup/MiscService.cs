// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;
using Microsoft.AspNetCore.Components.Authorization;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Interfaces;
using SnapCd.Server.Core.Services.Interfaces.DataSeeder;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Interfaces.QuotaUsage;
using SnapCd.Server.Core.Services.Interfaces.Resilience;
using SnapCd.Server.Core.Services.Interfaces.ViewManagement;
using SnapCd.Server.Core.Services.Nonsecured;
using SnapCd.Server.Core.Services.Nonsecured.AgentConnectionValidator;
using SnapCd.Server.Core.Services.Nonsecured.Crud.Jobs;
using SnapCd.Server.Core.Services.Nonsecured.Crud.Transfers;
using SnapCd.Server.Core.Services.Nonsecured.Dashboard;
using SnapCd.Server.Core.Services.Nonsecured.DataSeeder;
using SnapCd.Server.Core.Services.Nonsecured.DependencyGraph;
using SnapCd.Server.Core.Services.Nonsecured.Logs;
using SnapCd.Server.Core.Services.Nonsecured.MaintenanceMode;
using SnapCd.Server.Core.Services.Nonsecured.ModuleHold;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.ResolvedConfiguration;
using SnapCd.Server.Core.Services.Nonsecured.RippleProbes;
using SnapCd.Server.Core.Services.Nonsecured.SecretMigrator;
using SnapCd.Server.Core.Services.Nonsecured.ViewManagement;
using SnapCd.Server.Core.Services.Secured;
using SnapCd.Server.Core.Services.Secured.Crud.Jobs;
using SnapCd.Server.Core.Services.Secured.Logs;
using SnapCd.Server.Core.Services.Secured.ModuleHold;
using SnapCd.Server.Core.Services.Secured.RippleProbes;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Services.Utils.Dashboard;
using SnapCd.Server.Core.Services.Utils.Notification;
using SnapCd.Server.Core.Services.Utils.QuotaUsage;
using SnapCd.Server.Core.Services.Utils.Resilience;
using SnapCd.Server.Core.Settings;
using RunnerSelectionService = SnapCd.Server.Core.Services.Nonsecured.RunnerSelectionService;

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
        services.AddSingleton<ITransientFaultClassifier, TransientFaultClassifier>();

        services.AddScoped<AgentConnectionValidator>();

        services.AddScoped<ResolvedConfigurationService>();
        services.AddScoped<DependencyGraphService>();

        // Parameter Resolution (for Terraform)
        services.AddScoped<ParamResolverFactory>();

        // Idempotent SQL Management
        services.AddScoped<IIdempotentSqlManager, IdempotentSqlManager>();

        // Job Execution Services
        services.AddScoped<JobService>();
        services.AddScoped<SecuredJobService>();
        services.AddScoped<JobOrchestrationService>();
        services.AddScoped<ModuleHoldStore>();
        services.AddScoped<ModuleHoldService>();
        services.AddScoped<ModuleHoldGate>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<SourceChangedService>();

        // Authentication and Security Services
        services.AddScoped<AccessTokenService>();
        services.AddScoped<EmailConfirmationService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<OrganizationService>();

        // Notification Services (Singletons)
        services.AddSingleton<RoleCapabilitiesService>();
        services.AddSingleton<JobCreatedNotificationService>();
        services.AddSingleton<JobUpdatedNotificationService>();
        services.AddSingleton<StateMigrationUpdatedNotificationService>();
        services.AddSingleton<ProbeWaveUpdatedNotificationService>();
        services.AddSingleton<LogReceivedNotificationService>();
        services.AddSingleton<RunnerAvailabilityModifiedNotificationService>();
        services.AddSingleton<AgentAvailabilityModifiedNotificationService>();
        services.AddSingleton<MissionRunModifiedNotificationService>();
        services.AddSingleton<ModuleSagaModifiedNotificationService>();
        services.AddSingleton<ModuleStateModifiedNotificationService>();
        services.AddSingleton<ModuleJobApprovalModifiedNotificationService>();
        services.AddSingleton<ModuleApprovalThresholdModifiedNotificationService>();
        services.AddSingleton<LicenseUsageModifiedNotificationService>();
        services.AddSingleton<FavoritesModifiedNotificationService>();
        services.AddSingleton<ColorsModifiedNotificationService>();


        //services.AddScoped<ModuleJobApprovalRepository>(); // currently doesn't exist

        services.AddScoped<CustomOutputMapper>();

        // State Store
        services.AddOptions<StateStoreSettings>()
            .Bind(configuration.GetSection("StateStore"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IStateEncryptionService, StateEncryptionService>();
        services.AddScoped<TransferArtefactService>();
        services.AddScoped<StateMigrationStepService>();


        // Execution Services
        services.AddScoped<SourceRefreshJob>();
        services.AddScoped<SourceRefreshDispatcher>();

        services.AddScoped<JobLogWriterFactory>();
        services.AddScoped(typeof(JobLogReader<,,>));
        services.AddScoped<SecuredLogReaderFactory>();

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
        services.AddScoped<SecretMigratorService>();

        services.AddScoped<IQuotaGatingService, QuotaGatingService>();
        services.AddScoped<QuotaService>();
        services.AddScoped<StuckJobDetectionService>();
        services.AddScoped<RunnerConnectionStateMigrationService>();
        services.AddScoped<TransportProbeService>();

        // The probe step's query, inputs document and signal recording. RippleProbeConsumerLookup
        // is the output-to-variable map the inputs document is keyed by.
        services.AddScoped<RippleProbeConsumerLookupFactory>();
        services.AddScoped<ProbeInputsBuilderFactory>();
        services.AddScoped<ProbeReadinessReaderFactory>();
        services.AddScoped<ProbeWaveProgressReaderFactory>();
        services.AddScoped<ProbeWaveReadServiceFactory>();
        services.AddScoped<ProbeRerunServiceFactory>();
        services.AddScoped<ProbeWaveStarterFactory>();
        services.AddScoped<SignalEdgeResolverFactory>();
        services.AddScoped<ProbeSignalRecorderFactory>();
        // The services themselves, for anything resolved per scope: an activity, a consumer, the
        // harness. The factories above stay for a page that wants an instance it controls.
        services.AddScoped<RippleProbeConsumerLookup>();
        services.AddScoped<ProbeReadinessReader>();
        services.AddScoped<ProbeInputsBuilder>();
        services.AddScoped<ProbeWaveProgressReader>();
        services.AddScoped<ProbeWaveReadService>();
        services.AddScoped<ProbeRerunService>();
        services.AddScoped<ProbeWaveStarter>();
        services.AddScoped<SignalEdgeResolver>();
        services.AddScoped<ProbeSignalRecorder>();

        services.AddScoped<ISignalSecretReader, SignalSecretReader>();
        services.AddScoped<ISignalSecretWriter, SignalSecretWriter>();
        services.AddScoped<TransportReconciliationJob>();
        services.AddSingleton<IMaintenanceModeService, MaintenanceModeService>();
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
        services.AddScoped<IQuotaUsageForInvitationService, NoOpQuotaUsageForInvitationService>();
        services.AddScoped<IQuotaUsageForEmailConfirmationService, NoOpQuotaUsageForEmailConfirmationService>();
        services.AddScoped<IQuotaUsageForPasswordResetService, NoOpQuotaUsageForPasswordResetService>();
        services.AddScoped<IQuotaUsageForInvitationServiceFactory, NoOpQuotaUsageForInvitationServiceFactory>();
        services.AddScoped<IQuotaUsageForEmailConfirmationServiceFactory, NoOpQuotaUsageForEmailConfirmationServiceFactory>();
        services.AddSingleton<IQuotaUsageForPasswordResetServiceFactory, NoOpQuotaUsageForPasswordResetServiceFactory>();

        // No-op terms acceptance (self-hosted). SaaS Replace()s with the real DB-backed implementation.
        services.AddScoped<ITermsAcceptanceService, NoOpTermsAcceptanceService>();

        services.AddScoped<OrphanedJobCleanupService>();

        return services;
    }
}