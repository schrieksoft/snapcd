using System.Diagnostics;
using Microsoft.AspNetCore.Components.Authorization;
using SnapCd.Server.Core.Licensing.Filters;
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


        services.AddScoped<ResolvedConfigurationService>();
        services.AddScoped<DependencyGraphService>();

        // Parameter Resolution (for Terraform)
        services.AddScoped<ParamResolverFactory>();

        // Database View Management
        services.AddScoped<IViewManager, ViewManager>();

        // Job Execution Services
        services.AddScoped<JobService>();
        services.AddScoped<SecuredJobService>();
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
        
        if (System.Diagnostics.Debugger.IsAttached)
        {
            services.AddScoped<IDataSeeder, DebugDataSeeder>();
        }
        else
        {
            services.AddScoped<IDataSeeder, ProductionDataSeeder>();
        }


        // Secret Migrator
        services.AddScoped<Services.SecretMigrator.SecretMigratorService>();

        // Enterprise Edition services
        services.AddScoped<LicenseService>();
        services.AddScoped<LicenseRefreshJob>();
        services.AddScoped<LicensePublicKeyRefreshJob>();
        services.AddSingleton<ILicensePublicKeyService, LicensePublicKeyService>();
        services.AddScoped<ISaaSLicenseClient, SaaSLicenseClient>();
        services.AddScoped<VerifyLicenseActionFilter>();
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