// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;
using SnapCd.Server.Core.Startup;
using SnapCd.Server.Host.Database;
using SnapCd.Server.Host.CapAlerts;
using SnapCd.Server.Host.Installations;
using SnapCd.Server.Host.Telemetry;
using SnapCd.Server.Host.Licensing.Services;
using SnapCd.Server.Host.Services;

namespace SnapCd.JobRun;

/// <summary>
/// The server's own registrations, minus the web surface. Everything the job path touches is
/// registered from the same extension methods the server uses, so a run exercises the
/// composition the server actually has rather than an approximation of it.
/// </summary>
public static class ServerComposition
{
    public static void AddServerServices(
        IServiceCollection services, ConfigurationManager configuration, string connectionString)
    {
        services.AddOptions<SnapCd.Server.Core.Settings.ServerSettings>()
            .Bind(configuration.GetSection("Server"));
        services.AddOptions<SnapCd.Server.Core.Settings.ServiceBusSettings>()
            .Bind(configuration.GetSection("ServiceBus"));
        // Every options type the server binds in its own startup. They are bound here rather
        // than left to their defaults, which differ from appsettings in ways that fail late.
        services.Configure<ProductionDataSeederSettings>(configuration.GetSection("ProductionDataSeeder"));
        services.Configure<DebugDataSeederSettings>(configuration.GetSection("DebugDataSeeder"));
        services.Configure<SecretStoreSettings>(configuration.GetSection("SecretStore"));
        services.Configure<StateStoreSettings>(configuration.GetSection("StateStore"));
        services.Configure<SourceRefreshSettings>(configuration.GetSection("SourceRefresh"));
        services.Configure<InvitationSettings>(configuration.GetSection("InvitationSettings"));
        services.Configure<OrphanedJobCleanupSettings>(configuration.GetSection("OrphanedJobCleanup"));
        services.Configure<StuckJobDetectionSettings>(configuration.GetSection("StuckJobDetection"));
        services.Configure<LicenseSettings>(configuration.GetSection("License"));
        services.Configure<TelemetrySettings>(configuration.GetSection("Telemetry"));
        services.Configure<DebuggingOptions>(configuration.GetSection("Debugging"));
        services.AddSnapCdRepositorySettings(configuration);

        services.AddSelfHostedDbContextConfiguration(connectionString);
        services.AddSnapCdFactories();
        services.AddSnapCdSecuredRepositories();
        services.AddSnapCdRepositories();
        services.AddSnapCdCrudServices();
        services.AddSnapCdTaskHandlers();
        services.AddSnapCdMiscServices(configuration, isDevelopment: true);

        services.AddSnapCdCaching(configuration);
        services.AddHttpContextAccessor();

        services.AddSingleton<InstallationService>();
        services.AddSingleton<TelemetrySnapshotProvider>();
        services.AddSingleton<GitHubReleasesClient>();
        services.AddSingleton<ModuleCountChangedNotificationService>();
        services.AddScoped<CapAlertService>();
        services.AddScoped<TelemetryClient>();
        services.AddScoped<LicenseService>();
        services.AddSingleton<ILicensePublicKeyService, LicensePublicKeyService>();
        services.AddScoped<IRemoteLicenseClient, RemoteLicenseClient>();
        services.AddScoped<ILicenseInfoProvider>(sp => sp.GetRequiredService<LicenseService>());
        services.AddScoped<ILicenseVerificationPolicy, SelfHostedLicenseVerificationPolicy>();
        services.AddScoped<IPremiumMessageBrokerPolicy, LicensedPremiumMessageBrokerPolicy>();
        services.AddScoped<IPremiumSecretStorePolicy, LicensedPremiumSecretStorePolicy>();
        services.AddScoped<IPremiumEmailPolicy, LicensedPremiumEmailPolicy>();
        services.AddScoped<ISsoPolicy, SelfHostedSsoPolicy>();
        services.AddScoped<IOrganizationLimitPolicy, SelfHostedOrganizationLimitPolicy>();
        services.AddScoped<IOrganizationActivationService, AlwaysActivatedOrganizationActivationService>();
        services.AddScoped<IUserQuotaProvider, NoOpUserQuotaProvider>();

        // The seeder creates a User, a service principal and OAuth scopes, so Identity and
        // OpenIddict have to be registered in full even though nothing here signs in. It
        // resolves ISsoPolicy during registration, so the policies above come first.
        services.AddSnapCdAuthConfiguration(configuration, allowHttp: true);

        // Registers the runner-side services the dispatch and reply paths resolve. The hub
        // itself is never mapped: nothing listens, and the dispatch seam is replaced.
        services.AddSnapCdRunnerHub();

        services.AddSnapCdMassTransitConfiguration(configuration);
    }
}
