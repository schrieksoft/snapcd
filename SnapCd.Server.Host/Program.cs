// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Middleware;
using SnapCd.Server.Core.Misc.Configuration;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Host.Licensing.Filters;
using SnapCd.Server.Host.Licensing.Services;
using SnapCd.Server.Host.CapAlerts;
using SnapCd.Server.Host.Installations;
using SnapCd.Server.Host.Telemetry;
using SnapCd.Contracts.Logging;
using SnapCd.Server.Core.Services.Interfaces;
using SnapCd.Server.Core.Services.Interfaces.Admin;
using SnapCd.Server.Core.Services.Interfaces.DataSeeder;
using SnapCd.Server.Core.Services.Interfaces.Edition;
using SnapCd.Server.Core.Services.Interfaces.OrganizationContext;
using SnapCd.Server.Core.Services.Interfaces.ViewManagement;
using SnapCd.Server.Core.Services.Nonsecured;
using SnapCd.Server.Core.Services.Nonsecured.MaintenanceMode;
using SnapCd.Server.Core.Services.Nonsecured.OrganizationContext;
using SnapCd.Server.Core.Services.Nonsecured.Vaults;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Services.Utils.CallerContext;
using SnapCd.Server.Core.Services.Utils.Dashboard;
using SnapCd.Server.Core.Services.Utils.Edition;
using SnapCd.Server.Core.Services.Utils.OrganizationContext;
using SnapCd.Server.Core.Startup;
using SnapCd.Server.Host.Database;
using SnapCd.Server.Host.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSnapCdFileLogging(builder.Configuration);

builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true) // Service-specific overrides
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true) // Environment overrides
    .AddEnvironmentVariables() // Env vars override files
    .AddCommandLine(args)
    .AddExternalConfiguration() // Service-specific (sensitive) overrides, e.g. loaded directly from AKV
    .AddPredefined(); // Predefined values generated at startup

builder.Services.AddOptions<ServerSettings>()
    .Bind(builder.Configuration.GetSection("Server"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ServiceBusSettings>()
    .Bind(builder.Configuration.GetSection("ServiceBus"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<ServiceBusSettings>, SnapCd.Server.Core.Validation.ServiceBusSettingsValidator>();
builder.Services.Configure<ProductionDataSeederSettings>(builder.Configuration.GetSection("ProductionDataSeeder"));
builder.Services.Configure<DebugDataSeederSettings>(builder.Configuration.GetSection("DebugDataSeeder"));
builder.Services.AddOptions<SecretStoreSettings>()
    .Bind(builder.Configuration.GetSection("SecretStore"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<SecretStoreSettings>, SnapCd.Server.Core.Validation.SecretStoreSettingsValidator>();
builder.Services.AddSnapCdRepositorySettings(builder.Configuration);
builder.Services.Configure<SourceRefreshSettings>(builder.Configuration.GetSection("SourceRefresh"));
builder.Services.Configure<InvitationSettings>(builder.Configuration.GetSection("InvitationSettings"));
builder.Services.Configure<OrphanedJobCleanupSettings>(builder.Configuration.GetSection("OrphanedJobCleanup"));
builder.Services.Configure<StuckJobDetectionSettings>(builder.Configuration.GetSection("StuckJobDetection"));
builder.Services.Configure<JobLogSettings>(builder.Configuration.GetSection("JobLogs"));
builder.Services.Configure<ModuleHoldSettings>(builder.Configuration.GetSection("ModuleHold"));
builder.Services.Configure<SetRetentionSettings>(builder.Configuration.GetSection("SetRetention"));
builder.Services.Configure<LicenseSettings>(builder.Configuration.GetSection("License"));
builder.Services.Configure<TelemetrySettings>(builder.Configuration.GetSection("Telemetry"));
builder.Services.Configure<DebuggingOptions>(builder.Configuration.GetSection("Debugging"));
builder.Services.AddOptions<OpenIdConnectSettings>()
    .Bind(builder.Configuration.GetSection("OpenIdConnect"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

SettingsBindingCheck.Report(
    builder.Configuration,
    LoggerFactory.Create(c => c.AddConfiguration(builder.Configuration.GetSection("Logging")).AddConsole())
        .CreateLogger("SnapCd.Configuration"),
    ("Server", typeof(ServerSettings)),
    ("ServiceBus", typeof(ServiceBusSettings)),
    ("ProductionDataSeeder", typeof(ProductionDataSeederSettings)),
    ("DebugDataSeeder", typeof(DebugDataSeederSettings)),
    ("SecretStore", typeof(SecretStoreSettings)),
    ("SourceRefresh", typeof(SourceRefreshSettings)),
    ("InvitationSettings", typeof(InvitationSettings)),
    ("OrphanedJobCleanup", typeof(OrphanedJobCleanupSettings)),
    ("StuckJobDetection", typeof(StuckJobDetectionSettings)),
    ("JobLogs", typeof(JobLogSettings)),
    ("ModuleHold", typeof(ModuleHoldSettings)),
    ("SetRetention", typeof(SetRetentionSettings)),
    ("License", typeof(LicenseSettings)),
    ("Telemetry", typeof(TelemetrySettings)),
    ("Debugging", typeof(DebuggingOptions)),
    ("OpenIdConnect", typeof(OpenIdConnectSettings)),
    ("BackgroundJobs", typeof(BackgroundJobsSettings)));
// In non-debug runs, force LicenseServerBaseUrl to snapcd.io regardless of appsettings.
// Debug runs keep whatever was bound from appsettings (e.g. Development overrides to a local license server).
if (!Debugger.IsAttached)
{
    builder.Services.PostConfigure<LicenseSettings>(s => s.LicenseServerBaseUrl = "https://snapcd.io");
}
else if (!(builder.Configuration.GetSection("Debugging").Get<DebuggingOptions>()?.EnableTelemetryWhenDebuggerAttached ?? false))
{
    builder.Services.PostConfigure<TelemetrySettings>(s => s.Enabled = false);
}


var allowHttp = builder.Configuration.GetSection("AllowHttp").Get<bool>();
var connectionString = builder.Configuration["ConnectionString"] ?? throw new Exception("Connection string not found.");

builder.Services.AddSelfHostedDbContextConfiguration(connectionString);
builder.Services.AddSnapCdControllers([typeof(SnapCd.Server.Host.Controllers.LicenseController)]);
builder.Services.AddSnapCdEmailSender(builder.Configuration);
builder.Services.AddSnapCdFactories();
builder.Services.AddSnapCdSecuredRepositories();
builder.Services.AddSnapCdRepositories();
builder.Services.AddSnapCdCrudServices();
builder.Services.AddSnapCdTaskHandlers();
builder.Services.AddSnapCdMiscServices(builder.Configuration, builder.Environment.IsDevelopment());


// Self-Hosted licensing services (moved out of Server.Core because they query the
// SelfHostedOrganizationLicense entity that only the self-hosted DbContext registers).
builder.Services.AddScoped<LicenseService>();
builder.Services.AddScoped<LicenseServiceFactory>();
builder.Services.AddScoped<LicenseRefreshJob>();
builder.Services.AddScoped<LicensePublicKeyRefreshJob>();
builder.Services.AddSingleton<ILicensePublicKeyService, LicensePublicKeyService>();
builder.Services.AddScoped<IRemoteLicenseClient, RemoteLicenseClient>();
builder.Services.AddSingleton<InstallationService>();
builder.Services.AddSingleton<TelemetrySnapshotProvider>();
builder.Services.AddSingleton<GitHubReleasesClient>();
builder.Services.AddSingleton<ModuleCountChangedNotificationService>();
builder.Services.AddScoped<CapAlertServiceFactory>();
builder.Services.AddScoped<TelemetryClient>();
builder.Services.AddScoped<TelemetryReportJob>();
builder.Services.AddScoped<VerifyLicenseActionFilter>();
builder.Services.PostConfigure<Microsoft.AspNetCore.Mvc.MvcOptions>(o =>
    o.Filters.AddService<VerifyLicenseActionFilter>());

// Edition policies (self-hosted; must be after AddSnapCdMiscServices, before AddSnapCdAuthConfiguration)
builder.Services.AddScoped<IOrganizationLimitPolicy, SelfHostedOrganizationLimitPolicy>();
builder.Services.AddSingleton<IOrganizationSwitchingPolicy, SelfHostedOrganizationSwitchingPolicy>();
builder.Services.AddScoped<ILicenseVerificationPolicy, SelfHostedLicenseVerificationPolicy>();
builder.Services.AddScoped<ILicenseInfoProvider>(sp => sp.GetRequiredService<LicenseService>());
builder.Services.AddScoped<ISsoPolicy, SelfHostedSsoPolicy>();
builder.Services.AddScoped<ITurnstilePolicy, SelfHostedTurnstilePolicy>();
builder.Services.AddScoped<IPremiumEmailPolicy, LicensedPremiumEmailPolicy>();
builder.Services.AddScoped<IPremiumMessageBrokerPolicy, LicensedPremiumMessageBrokerPolicy>();
builder.Services.AddScoped<IPremiumSecretStorePolicy, LicensedPremiumSecretStorePolicy>();
builder.Services.AddScoped<IOrganizationActivationService, AlwaysActivatedOrganizationActivationService>();
builder.Services.AddSingleton<ISelfRegistrationPolicy, SelfHostedSelfRegistrationPolicy>();
builder.Services.AddSingleton<IInvitationAutoAcceptPolicy, SelfHostedInvitationAutoAcceptPolicy>();
builder.Services.AddSingleton<IConsentRequirementPolicy, SelfHostedConsentRequirementPolicy>();
builder.Services.AddSingleton<IPostOrganizationCreateRedirectPolicy, SelfHostedPostOrganizationCreateRedirectPolicy>();
builder.Services.AddSingleton<IOrganizationNotActivatedResultProvider, NoOpOrganizationNotActivatedResultProvider>();
builder.Services.AddSingleton<IOrganizationCountValidator, SelfHostedOrganizationCountValidator>();
builder.Services.AddSingleton<SelfHostedOrganizationIdProvider>();
builder.Services.AddScoped<IUserQuotaProvider, NoOpUserQuotaProvider>();

// AddSnapCdAuthConfiguration resolves ISsoPolicy -> LicenseService -> IPrincipalProvider ->
// HttpContextPrincipalProvider during registration, so the accessor must already be registered.
builder.Services.AddHttpContextAccessor();

builder.Services.AddSnapCdAuthConfiguration(builder.Configuration, allowHttp);

builder.Services.AddSnapCdBackgroundJobs(connectionString);
builder.Services.AddSnapCdScalarConfiguration(builder.Configuration);
builder.Services.AddSnapCdCorsConfiguration();
builder.Services.AddSnapCdMassTransitConfiguration(builder.Configuration, [typeof(ModuleCountChangedFanoutConsumer)]);
builder.Services.AddSnapCdRunnerHub();
builder.Services.AddSnapCdMcpServer();
builder.Services.AddSnapCdHeaderForwardingConfiguration();
    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMudServices();
SnapCd.Server.Core.UI.Dashboard.Theme.SnapCdTheme.ApplyInputDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSnapCdCaching(builder.Configuration);
builder.Services.AddTurnstileServices(builder.Configuration);
builder.Services.AddRazorPages();
builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();
builder.Services.AddSingleton<OrganizationMembershipCacheService>();
builder.Services.AddScoped<IEditionNavProvider, ServerEditionNavProvider>();
builder.Services.AddScoped<IMemberAdminService, SelfHostedMemberAdminService>();
builder.Services.AddScoped<IOrganizationMemberAdminActionsProvider, ServerOrganizationMemberAdminActionsProvider>();

// ValidateOnBuild walks every registration at startup, so a missing one fails the deploy rather
// than the first request that needs it. Scope validation checks every resolve, so it is a
// development-only cost.
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
});

var app = builder.Build();

// Repositories are often constructed by hand, so the write gate is static and armed here once.
var maintenanceModeService = app.Services.GetRequiredService<IMaintenanceModeService>();
MaintenanceGate.Initialize(maintenanceModeService);

// Eagerly validate options before migrations / data seeding run.
// ValidateOnStart() registers a hosted service that fires too late — after the inline
// startup block below. Resolving IOptions<T>.Value here triggers the same
// ValidateDataAnnotations + IValidateOptions<T> pipeline immediately. We collect all
// failures so the operator sees every missing setting in one message.
{
    var validationErrors = new List<string>();

    void ValidateOptions<T>(IServiceProvider sp) where T : class
    {
        try { _ = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<T>>().Value; }
        catch (Microsoft.Extensions.Options.OptionsValidationException ex) { validationErrors.AddRange(ex.Failures); }
    }

    ValidateOptions<ServerSettings>(app.Services);
    ValidateOptions<StateStoreSettings>(app.Services);
    ValidateOptions<OpenIdConnectSettings>(app.Services);
    ValidateOptions<ServiceBusSettings>(app.Services);
    ValidateOptions<SecretStoreSettings>(app.Services);
    ValidateOptions<CachingSettings>(app.Services);

    if (validationErrors.Count > 0)
        throw new Microsoft.Extensions.Options.OptionsValidationException(
            "MultipleSettings",
            typeof(object),
            validationErrors);
}

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SnapCdDbContext>(); 
    await dbContext.Database.MigrateAsync();

    // Apply idempotent SQL scripts after migrations
    var idempotentSqlManager = scope.ServiceProvider.GetRequiredService<IIdempotentSqlManager>();
    await idempotentSqlManager.ApplyIdempotentSqlAsync();

    var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    using (CallerContext.Begin(CallerKind.System))
    {
        await dataSeeder.SeedAsync();
    }

    // Validate organization limit for self-hosted edition
    var userOrgCount = await dbContext.Organizations
        .CountAsync(o => o.DeletedDateTime == null
                         && o.Id != PreseededSettings.DefaultId);
    if (userOrgCount > 1)
    {
        var message = $"""
            FATAL: Self-hosted edition supports only 1 organization, but {userOrgCount} were found in the database.

            To fix this, delete the extra organizations from the database, leaving only the one you want to keep.
            If this is a fresh install or development environment, you can reset the database by running:

                make down && make up

            Then restart the server.
            """;
        Console.Error.WriteLine(message);
        throw new InvalidOperationException(message);
    }
}

// The flag cache has no expiry, so each boot rewrites it from the table the migration creates.
await maintenanceModeService.SyncCacheAsync();

var jobLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SnapCd.BackgroundJobs");
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
var backgroundJobs = app.Services.GetRequiredService<IBackgroundJobClient>();

RecurringJobs.RegisterCoreJobs(recurringJobs, backgroundJobs, builder.Configuration, jobLogger);

T JobSettings<T>(string name) where T : BackgroundJobSettingsBase, new() =>
    RecurringJobs.Settings<T>(builder.Configuration, name);

// Licensing and telemetry are this host's own.
RecurringJobs.Register<LicenseRefreshJob>(recurringJobs, JobSettings<LicenseRefreshJobSettings>("LicenseRefresh"),
    "license-refresh-job", x => x.ExecuteJob(), jobLogger);

RecurringJobs.Register<LicensePublicKeyRefreshJob>(recurringJobs, JobSettings<LicensePublicKeyRefreshJobSettings>("LicensePublicKeyRefresh"),
    "license-public-key-refresh-job", x => x.ExecuteJob(), jobLogger);

RecurringJobs.Register<TelemetryReportJob>(recurringJobs, JobSettings<TelemetryReportJobSettings>("TelemetryReport"),
    "telemetry-report-job", x => x.ExecuteJob(), jobLogger);
// One run shortly after boot, so a fresh install or an upgrade has notices and the version indicator today, not tomorrow.
BackgroundJob.Schedule<TelemetryReportJob>(x => x.ExecuteJob(), TimeSpan.FromMinutes(1));

// Serves the OpenAPI document at /openapi/v1.json — consumed by the
// Scalar reference (/ApiReference).
app.MapOpenApi();
if (Debugger.IsAttached)
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    if (!allowHttp)
        app.UseHsts();
}

if (!allowHttp)
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<SnapCd.Server.Host.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(SnapCd.Server.Core.Marker).Assembly);
app.MapAdditionalIdentityEndpoints(); // Add additional endpoints required by the Identity / Account Razor components.
app.MapAdditionalFormEndpoints();
app.MapApiReferenceStandalone();
app.UseRouting();
app.UseCors("AllowAnyOriginCorsPolicy");
app.UseAuthentication();
app.UseMiddleware<SnapCd.Server.Core.Auth.AgentClaimAuditMiddleware>();
app.UseAuthorization();

app.UseSnapCdHangfireDashboard();
app.UseMiddleware<MaintenanceModeMiddleware>();
app.UseMiddleware<OrganizationValidationMiddleware>();
app.UseAntiforgery();
app.UseMiddleware<TransientFaultRetryMiddleware>();
app.MapControllers();
//app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapHealthChecks("/health");
// SignalR authenticates once per connection, so without this a hub connection outlives the token
// it was established with and every invocation is rejected while the connection still looks live.
app.MapHub<RunnerHub>("/runnerhub", options => options.CloseOnAuthenticationExpiration = true);
app.MapHub<SnapCd.Server.Core.Hubs.AgentHub>("/agenthub", options => options.CloseOnAuthenticationExpiration = true);
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/mcp"),
    branch => branch.Use(async (ctx, next) =>
    {
        var orgClaim = ctx.User.FindFirst(SnapCd.Server.Core.Misc.Constants.ClaimTypeConstants.OrganizationClaimType)?.Value;
        var firstOrg = orgClaim?.Split(',').FirstOrDefault();
        if (!Guid.TryParse(firstOrg, out var orgId))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        var licenseInfoProvider = ctx.RequestServices.GetRequiredService<SnapCd.Server.Core.Licensing.Services.ILicenseInfoProvider>();
        var info = await licenseInfoProvider.GetLicenseInfoAsync(orgId);
        if (!info.Includes(SnapCd.Server.Core.Licensing.Models.Feature.AiAgents))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("AI Agents are not licensed for this organization. Upgrade to Enterprise to use the MCP server.");
            return;
        }
        await next();
    }));
app.MapMcp("/mcp").RequireAuthorization("BearerPolicy");

// Dev-only mission wiring harness: publish a synthetic ApplyModuleFailed for an existing ModuleJob,
// so the agent dispatch path (Layer 1 → ModuleJobMission → orchestrator → sidecar) can be exercised
// without a real failing terraform apply. The moduleId/jobId must reference existing rows
// (ModuleJobMission FKs to ModuleJob); organizationId defaults to the preseeded default org.
if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/agent/synthesize-apply-failed",
        async (Guid moduleId, Guid jobId, Guid? organizationId, MassTransit.IBus bus) =>
        {
            var orgId = organizationId ?? SnapCd.Server.Core.Settings.DataSeeder.PreseededSettings.DefaultId;
            await bus.Publish(new SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleFailed
            {
                ModuleId = moduleId,
                OrganizationId = orgId,
                ModuleJobId = jobId
            });
            return Results.Ok(new { published = "ApplyModuleFailed", moduleId, jobId, organizationId = orgId });
        });
}

var versionService = app.Services.GetRequiredService<IVersionService>();
app.Logger.LogInformation("Starting Snap CD Server v{Version}", versionService.Version);

app.Run();