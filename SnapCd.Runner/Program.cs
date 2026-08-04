// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using Quartz;
using SnapCd.Runner.Configuration;
using SnapCd.Runner.Factories;
using SnapCd.Runner.Hub;
using SnapCd.Runner.Logging;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleSourceRefresher;
using SnapCd.Runner.Settings;
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
builder.Services.AddOptions<ServerSettings>()
    .Bind(builder.Configuration.GetSection("Server"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<WorkingDirectorySettings>(builder.Configuration.GetSection("WorkingDirectory"));
builder.Services.AddOptions<RunnerSettings>()
    .Bind(builder.Configuration.GetSection("Runner"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<HooksPreapprovalSettings>(builder.Configuration.GetSection("HooksPreapproval"));
builder.Services.Configure<EngineSettings>(builder.Configuration.GetSection("Engine"));
builder.Services.Configure<JobLogStreamSettings>(builder.Configuration.GetSection("JobLogStream"));
builder.Services.Configure<RunnerEnvVarsSettings>(builder.Configuration.GetSection("RunnerEnvVars"));

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<AccessTokenCacheService>();
builder.Services.AddSingleton<GitFactory>();
builder.Services.AddSingleton<EngineFactory>();
// ParamResolverFactory removed - parameter resolution now happens on server before dispatching
builder.Services.AddSingleton<VariableDiscoveryServiceFactory>();
builder.Services.AddSingleton<ModuleGetterFactory>();
builder.Services.AddSingleton<IModuleSourceRefresherFactory, ModuleSourceRefresherFactory>();
builder.Services.Configure<SourceCacheSettings>(builder.Configuration.GetSection("SourceCache"));
builder.Services.Configure<PolicyEvaluationSettings>(builder.Configuration.GetSection("PolicyEvaluation"));
builder.Services.AddSingleton<BareCloneCache>();
builder.Services.AddSingleton<SnapCd.Runner.Services.PolicyEvaluation.PolicyEvaluationService>();
builder.Services.AddSingleton<SnapCdInspect>();


// HTTP clients removed - runner no longer makes API calls back to server
// All data is now sent via SignalR

builder.Services.AddSingleton<ProcessRegistry>();

// Register unified task handler. Also expose as Lazy<Tasks> to let RunnerHubConnection break
// the DI cycle: Tasks -> IJobLogStream -> HubJobLogStream -> RunnerHubConnection -> Tasks.
// The hub only dereferences Tasks inside .On<>() handlers fired after StartAsync, by which
// time the graph is fully built.
builder.Services.AddSingleton<Tasks>();
builder.Services.AddSingleton<Lazy<Tasks>>(sp => new Lazy<Tasks>(() => sp.GetRequiredService<Tasks>()));

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

// Terminal logging — vanilla MEL via the host default (Console + Debug providers configured from
// the standard "Logging" section in appsettings.json). Matches SnapCd.Server.Host + SnapCd.Agent.

// Server-shipping — IJobLogStream is the explicit "this log ships" producer surface. HubJobLogStream
// owns a private Serilog logger + PeriodicBatchingSink + SignalRLogSink. Serilog is scoped to
// HubJobLogStream only; the rest of the runner is on MEL.
builder.Services.AddSingleton<IJobLogStream, HubJobLogStream>();

builder.Services.AddHttpClient<ServicePrincipalTokenService>();
builder.Services.AddSingleton<TokenInitializationService>();

# endregion

var app = builder.Build();

// Block startup until token is obtained
using var scope = app.Services.CreateScope();
var tokenInitializer = scope.ServiceProvider.GetRequiredService<TokenInitializationService>();
await tokenInitializer.InitializeAsync();


var versionService = app.Services.GetRequiredService<IVersionService>();
var serverSettings = app.Services.GetRequiredService<IOptions<ServerSettings>>().Value;

app.Logger.LogInformation(
    "Starting SnapCD Runner v{Version}. Connecting to {ServerUrl}",
    versionService.Version, serverSettings.Url);

await app.RunAsync();
