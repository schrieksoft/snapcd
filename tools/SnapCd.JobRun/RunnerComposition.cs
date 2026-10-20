// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Runner.Factories;
using SnapCd.Runner.Logging;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleSourceRefresher;
using SnapCd.Runner.Services.PolicyEvaluation;
using SnapCd.Runner.Settings;
using SnapCd.Runner.Tasks;

namespace SnapCd.JobRun;

/// <summary>
/// The runner's own services, so a run can call the real task code rather than a canned reply.
///
/// Mirrors SnapCd.Runner's Program.cs without its SignalR connection or hosted services: the
/// dispatch arrives from the server in-process, so there is nothing to connect to and no session
/// to keep alive. Everything that does the work - the engine, the module getter, the policy
/// evaluator - is registered exactly as the runner registers it.
/// </summary>
public static class RunnerComposition
{
    public static void AddRunnerServices(
        IServiceCollection services,
        IConfiguration configuration,
        string workingDirectory)
    {
        services.AddMemoryCache();

        services.Configure<RunnerSettings>(configuration.GetSection("Runner"));

        // The run owns its own tree: a module getter, an engine and a plan file all land under
        // here, and a run must not touch a real runner's directory.
        services.Configure<WorkingDirectorySettings>(options =>
        {
            options.WorkingDirectory = workingDirectory;
            options.TempDirectory = Path.Combine(workingDirectory, ".temp");
        });

        services.Configure<HooksPreapprovalSettings>(configuration.GetSection("HooksPreapproval"));
        services.Configure<EngineSettings>(configuration.GetSection("Engine"));
        services.Configure<JobLogStreamSettings>(configuration.GetSection("JobLogStream"));
        services.Configure<RunnerEnvVarsSettings>(configuration.GetSection("RunnerEnvVars"));
        services.Configure<SourceCacheSettings>(configuration.GetSection("SourceCache"));
        services.Configure<PolicyEvaluationSettings>(configuration.GetSection("PolicyEvaluation"));

        services.AddSingleton<AccessTokenCacheService>();
        services.AddSingleton<GitFactory>();
        services.AddSingleton<EngineFactory>();
        services.AddSingleton<VariableDiscoveryServiceFactory>();
        services.AddSingleton<ModuleGetterFactory>();
        services.AddSingleton<IModuleSourceRefresherFactory, ModuleSourceRefresherFactory>();
        services.AddSingleton<BareCloneCache>();
        services.AddSingleton<PolicyEvaluationService>();
        services.AddSingleton<SnapCdInspect>();
        services.AddSingleton<ProcessRegistry>();
        services.AddSingleton<HookPreapprovalService>();
        services.AddSingleton<IVersionService, VersionService>();

        // The runner streams its logs over its hub connection, which a run does not have. These
        // go to the console instead, where the rest of the run's output already is.
        services.AddSingleton<IJobLogStreams, ConsoleJobLogStreams>();

        services.AddSingleton<Tasks>();
    }
}

/// <summary>Job logs written where the run's own output goes.</summary>
public class ConsoleJobLogStreams : IJobLogStreams
{
    public IJobLogStream ModuleJobs { get; } = new ConsoleJobLogStream();
    public IJobLogStream StateMigrations { get; } = new ConsoleJobLogStream();
}

public class ConsoleJobLogStream : IJobLogStream
{
    public Task EmitAsync(JobLogEnvelope envelope, string message, CancellationToken ct = default)
    {
        Console.WriteLine($"     | {envelope.TaskName}: {message}");
        return Task.CompletedTask;
    }
}
