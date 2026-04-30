using System.Reflection;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using Serilog.Sinks.PeriodicBatching;
using SnapCd.Runner.Factories;
using SnapCd.Runner.Hub;
using SnapCd.Runner.Logging;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleSourceRefresher;
using SnapCd.Runner.Settings;
using SnapCd.Runner.Configuration;
using SnapCd.Runner.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .AddExternalConfiguration();

// builder.Services.Configure<ProviderCacheSettings>(builder.Configuration.GetSection("ProviderCache"));
builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("Server"));
builder.Services.Configure<WorkingDirectorySettings>(builder.Configuration.GetSection("WorkingDirectory"));
builder.Services.Configure<RunnerSettings>(builder.Configuration.GetSection("Runner"));
builder.Services.Configure<HooksPreapprovalSettings>(builder.Configuration.GetSection("HooksPreapproval"));
builder.Services.Configure<EngineSettings>(builder.Configuration.GetSection("Engine"));

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<AccessTokenCacheService>();
builder.Services.AddSingleton<GitFactory>();
builder.Services.AddSingleton<EngineFactory>();
// ParamResolverFactory removed - parameter resolution now happens on server before dispatching
builder.Services.AddSingleton<VariableDiscoveryServiceFactory>();
builder.Services.AddSingleton<ModuleGetterFactory>();
builder.Services.AddSingleton<IModuleSourceRefresherFactory, ModuleSourceRefresherFactory>();


// HTTP clients removed - runner no longer makes API calls back to server
// All data is now sent via SignalR

builder.Services.AddSingleton<ProcessRegistry>();

// Register unified task handler
builder.Services.AddSingleton<Tasks>();

// Register SignalR runner hub connection
builder.Services.AddSingleton<RunnerHubConnection>();

// Add a hosted service to start the SignalR connection
builder.Services.AddHostedService<RunnerSessionHostedService>();


// Add Version service
builder.Services.AddSingleton<IVersionService, VersionService>();

// Add Hooks Pre-approval service
builder.Services.AddSingleton<HookPreapprovalService>();

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
builder.Services.AddQuartz(q =>
{
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();

    q.AddJob<AccessTokenCacheQuartzJob>(opts => opts.WithIdentity(nameof(AccessTokenCacheQuartzJob)));
    q.AddTrigger(opts => opts
        .ForJob(nameof(AccessTokenCacheQuartzJob))
        .WithIdentity($"{nameof(AccessTokenCacheQuartzJob)}Immediate")
        .WithSimpleSchedule(x => x.WithRepeatCount(0))
        .StartAt(DateTimeOffset.UtcNow.AddMinutes(5)));
});

# region Logging

builder.Host.UseSerilog();

builder.Services.Configure<RunnerSettings>(builder.Configuration.GetSection("Runner"));
builder.Services.AddSingleton<AccessTokenCacheService>();
builder.Services.AddSingleton<IBatchedLogEventSink, SignalRLogSink>();
builder.Services.AddHttpClient<ServicePrincipalTokenService>();

builder.Services.AddSingleton<TokenInitializationService>();

// Note: ILogEmitter (RunnerHubConnection) must be registered by the consuming application

# endregion

var loggingSettings = builder.Configuration.GetSection("Logging").Get<LoggingSettings>() ?? new LoggingSettings();

var app = builder.Build();

// Block startup until token is obtained
using var scope = app.Services.CreateScope();
var tokenInitializer = scope.ServiceProvider.GetRequiredService<TokenInitializationService>();
await tokenInitializer.InitializeAsync();


var versionService = app.Services.GetRequiredService<IVersionService>();
var serverSettings = app.Services.GetRequiredService<IOptions<ServerSettings>>().Value;

// Retrieve IBatchedLogEventSink from DI
var sink = app.Services.GetRequiredService<IBatchedLogEventSink>();

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Is(loggingSettings.SystemDefaultLogLevel)
    .MinimumLevel.Override("SnapCd", loggingSettings.SnapCdDefaultLogLevel);
foreach (var obj in loggingSettings.LogLevelOverrides) loggerConfig.MinimumLevel.Override(obj.Key, obj.Value);

var batchOptions = new PeriodicBatchingSinkOptions
{
    BatchSizeLimit = loggingSettings.BatchSizeLimit,
    Period = TimeSpan.FromSeconds(loggingSettings.PeriodSeconds),
    EagerlyEmitFirstEvent = loggingSettings.EarlyEmitFirstEvent
};

var batchingSink = new PeriodicBatchingSink(sink, batchOptions);
loggerConfig.WriteTo.Sink(batchingSink);
loggerConfig.WriteTo.CustomConsole();

Log.Logger = loggerConfig.CreateLogger();

Console.WriteLine($"Starting SnapCD Runner v{versionService.Version}. Connecting to {serverSettings.Url}");

await app.RunAsync();