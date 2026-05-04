using System.Diagnostics;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Middleware;
using SnapCd.Server.Core.Misc.Configuration;
using SnapCd.Server.Core.Misc.Logging;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Dashboard;
using SnapCd.Server.Core.Services.DataSeeder;
using SnapCd.Server.Core.Services.OrganizationContext;
using SnapCd.Server.Core.Services.QuotaUsage;
using SnapCd.Server.Core.Services.ViewManagement;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Core.Startup;
using SnapCd.Server.SelfHosted.Database;
using SnapCd.Server.SelfHosted.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true) // Service-specific overrides
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true) // Environment overrides
    .AddEnvironmentVariables() // Env vars override files
    .AddCommandLine(args)
    .AddExternalConfiguration() // Service-specific (sensitive) overrides, e.g. loaded directly from AKV
    .AddPredefined(); // Predefined values generated at startup

builder.Host.UseSerilog();

builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("Server"));
builder.Services.Configure<SqlOutputStoreSettings>(builder.Configuration.GetSection("SqlOutputStore"));
builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("ServiceBus"));
builder.Services.Configure<DashboardSettings>(builder.Configuration.GetSection("Dashboard"));
builder.Services.Configure<ProductionDataSeederSettings>(builder.Configuration.GetSection("ProductionDataSeeder"));
builder.Services.Configure<DebugDataSeederSettings>(builder.Configuration.GetSection("DebugDataSeeder"));
builder.Services.Configure<SecretStoreSettings>(builder.Configuration.GetSection("SecretStore"));
builder.Services.Configure<SourceRefreshSettings>(builder.Configuration.GetSection("SourceRefresh"));
builder.Services.Configure<PreviewFeatureSettings>(builder.Configuration.GetSection("PreviewFeatures"));
builder.Services.AddSnapCdRepositorySettings(builder.Configuration);
builder.Services.Configure<InvitationSettings>(builder.Configuration.GetSection("InvitationSettings"));
builder.Services.Configure<OrphanedJobCleanupSettings>(builder.Configuration.GetSection("OrphanedJobCleanup"));
builder.Services.Configure<LicenseSettings>(builder.Configuration.GetSection("License"));
builder.Services.Configure<DebuggingOptions>(builder.Configuration.GetSection("Debugging"));
// In non-debug runs, force LicenseServerBaseUrl to snapcd.io regardless of appsettings.
// Debug runs keep whatever was bound from appsettings (e.g. Development overrides to a local license server).
if (!Debugger.IsAttached)
{
    builder.Services.PostConfigure<LicenseSettings>(s => s.LicenseServerBaseUrl = "https://snapcd.io");
}


var sourceRefreshSettings = builder.Configuration.GetSection("SourceRefresh").Get<SourceRefreshSettings>() ?? new SourceRefreshSettings();
var loggingSettings = builder.Configuration.GetSection("Logging").Get<LoggingSettings>() ?? new LoggingSettings();
var allowHttp = builder.Configuration.GetSection("AllowHttp").Get<bool>();
var connectionString = builder.Configuration["ConnectionString"] ?? throw new Exception("Connection string not found.");

builder.Services.AddSelfHostedDbContextConfiguration(connectionString);
builder.Services.AddSnapCdControllers();
builder.Services.AddSnapCdEmailSender(builder.Configuration);
builder.Services.AddSnapCdFactories();
builder.Services.AddSnapCdSecuredRepositories();
builder.Services.AddSnapCdRepositories();
builder.Services.AddSnapCdCrudServices();
builder.Services.AddSnapCdTaskHandlers();
builder.Services.AddSnapCdMiscServices(builder.Configuration, builder.Environment.IsDevelopment());


// Edition policies (self-hosted; must be after AddSnapCdMiscServices, before AddSnapCdAuthConfiguration)
builder.Services.AddScoped<IOrganizationLimitPolicy, SelfHostedOrganizationLimitPolicy>();
builder.Services.AddScoped<ILicenseVerificationPolicy, SelfHostedLicenseVerificationPolicy>();
builder.Services.AddScoped<ISsoPolicy, SelfHostedSsoPolicy>();
builder.Services.AddScoped<ITurnstilePolicy, SelfHostedTurnstilePolicy>();
builder.Services.AddScoped<IApprovalPolicy, SelfHostedApprovalPolicy>();
builder.Services.AddSingleton<IOrganizationCountValidator, SelfHostedOrganizationCountValidator>();
builder.Services.AddSingleton<SelfHostedOrganizationIdProvider>();
builder.Services.AddScoped<IUserQuotaProvider, NoOpUserQuotaProvider>();

builder.Services.AddSnapCdAuthConfiguration(builder.Configuration, allowHttp);

builder.Services.AddSnapCdBackgroundJobs(connectionString);
builder.Services.AddSnapCdSwaggerConfiguration(builder.Configuration);
builder.Services.AddSnapCdCorsConfiguration();
builder.Services.AddSnapCdMassTransitConfiguration(builder.Configuration);
builder.Services.AddSnapCdRunnerHub();
builder.Services.AddSnapCdHeaderForwardingConfiguration();
    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMudServices();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSnapCdCaching(builder.Configuration);
builder.Services.AddTurnstileServices(builder.Configuration);
builder.Services.AddRazorPages();
builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();
builder.Services.AddSingleton<OrganizationMembershipCacheService>();
builder.Services.AddScoped<IEditionNavProvider, ServerEditionNavProvider>();

var app = builder.Build();

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Is(loggingSettings.SystemDefaultLogLevel)
    .MinimumLevel.Override("SnapCd", loggingSettings.SnapCdDefaultLogLevel);
foreach (var obj in loggingSettings.LogLevelOverrides) loggerConfig.MinimumLevel.Override(obj.Key, obj.Value);
// .Filter.ByIncludingOnly(logEvent =>
//     logEvent.Properties.TryGetValue("SourceContext", out var source) &&
//     source.ToString().StartsWith("\"SnapCd"));

loggerConfig.WriteTo.CustomConsole();

Log.Logger = loggerConfig.CreateLogger();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SnapCdDbContext>();
    await dbContext.Database.MigrateAsync();

    // Apply database views after migrations
    var viewManager = scope.ServiceProvider.GetRequiredService<IViewManager>();
    await viewManager.ApplyViewsAsync();

    var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await dataSeeder.SeedAsync();

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
        Log.Fatal(message);
        throw new InvalidOperationException(message);
    }
}


app.UseHangfireDashboard();

RecurringJob.AddOrUpdate<SourceRefreshJob>(
    "source-refresh-job",
    x => x.ExecuteJob(),
    sourceRefreshSettings.RefreshIntervalCronExpression
);
RecurringJob.AddOrUpdate<ServerConnectionCleanupJob>(
    "server-connection-cleanup-job",
    x => x.ExecuteJob(),
    "*/1 * * * *" // Every 1 minute
);
RecurringJob.AddOrUpdate<RunnerConnectionJobCleanupJob>(
    "runner-connection-job-cleanup-job",
    x => x.ExecuteJob(),
    "*/15 * * * *" // Every 15 minutes
);

var invitationSettings = builder.Configuration.GetSection("InvitationSettings").Get<InvitationSettings>() ?? new InvitationSettings();
RecurringJob.AddOrUpdate<InvitationCleanupJob>(
    "invitation-cleanup-job",
    x => x.ExecuteJob(),
    invitationSettings.CleanupJobCron
);

var orphanedJobCleanupSettings = builder.Configuration.GetSection("OrphanedJobCleanup").Get<OrphanedJobCleanupSettings>() ?? new OrphanedJobCleanupSettings();
RecurringJob.AddOrUpdate<OrphanedJobCleanupJob>(
    "orphaned-job-cleanup-job",
    x => x.ExecuteJob(),
    orphanedJobCleanupSettings.CleanupCronExpression
);

var licenseSettings = builder.Configuration.GetSection("License").Get<LicenseSettings>() ?? new LicenseSettings();
RecurringJob.AddOrUpdate<LicenseRefreshJob>(
    "license-refresh-job",
    x => x.ExecuteJob(),
    licenseSettings.RefreshJobCron
);
RecurringJob.AddOrUpdate<LicensePublicKeyRefreshJob>(
    "license-public-key-refresh-job",
    x => x.ExecuteJob(),
    "0 4 * * *" // daily at 04:00 UTC
);

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SnapCd Api");
    c.OAuthClientId("SwaggerClient");
    c.OAuthUsePkce();
});
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
app.MapRazorComponents<SnapCd.Server.SelfHosted.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(SnapCd.Server.Core.Marker).Assembly);
app.MapAdditionalIdentityEndpoints(); // Add additional endpoints required by the Identity / Account Razor components.
app.MapAdditionalFormEndpoints();
app.UseRouting();
app.UseCors("AllowAnyOriginCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<OrganizationValidationMiddleware>();
app.UseAntiforgery();
app.MapControllers();
//app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapHealthChecks("/health");
app.MapHub<RunnerHub>("/runnerhub");

var versionService = app.Services.GetRequiredService<IVersionService>();
Console.WriteLine($"Starting SnapCD Server v{versionService.Version}");
Log.Information("Starting SnapCD Server v{Version}", versionService.Version);

app.Run();