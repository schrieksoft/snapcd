// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Endpoints;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Runner.Logging;
using SnapCd.Runner.Settings;
using SnapCd.Server.Core.Hubs;
using RunnerTasks = SnapCd.Runner.Tasks.Tasks;

namespace SnapCd.JobRun;

/// <summary>
/// Answers the runner's steps with the runner's own code, so a run plans and applies for real.
///
/// Only GetModule is faked, by copying an uninitialised fixture Module into the working directory
/// rather than cloning it. Everything after that - init, validate, plan, apply, probe, output - is
/// the engine doing the work, so a run says whether the inputs document the server built is one
/// the probe accepts, which a canned reply cannot.
///
/// Slower than the fully fake runner by minutes, and that is the trade: the fake one stays the
/// fast path for every job family that has nothing to do with propagation.
/// </summary>
public class SemiFakeRunner(
    IServiceProvider services,
    string connectionId,
    Guid organizationId,
    string instanceName,
    Action<string> trace,
    string fixtureRoot)
    : FakeRunner(services, connectionId, organizationId, instanceName, trace)
{
    /// <summary>
    /// A connection object the tasks carry but never call through: the only member any of them
    /// touches is State, read to decide whether a failed reply is worth retrying. It is never
    /// started, so no request is made to the address below.
    /// </summary>
    private readonly HubConnection _unconnected = new HubConnectionBuilder()
        .WithUrl("http://localhost:1/jobrun-never-connects")
        .Build();

    protected override async Task Reply(RunnerHub hub, string endpoint, object payload, Guid jobId)
    {
        var tasks = services.GetRequiredService<RunnerTasks>();
        var logStream = services.GetRequiredService<IJobLogStreams>().ModuleJobs;

        switch (endpoint)
        {
            // Faked: a fixture Module is already on disk, so there is nothing to clone. Copied
            // rather than pointed at, because init and plan write into the directory and the
            // fixture has to stay clean for the next run.
            case nameof(IApplyEndpoints.ApplyGetModule):
                PlaceFixtureModule((GetModuleRequestBase)payload);
                await hub.ApplyGetModuleCompleted(jobId);
                break;

            case nameof(IApplyEndpoints.ApplyInit):
                await tasks.Init(
                    (InitRequestBase)payload, _unconnected, logStream,
                    id => hub.ApplyInitCompleted(id),
                    id => hub.ApplyInitCancelled(id),
                    (id, error, stack) => hub.ApplyInitFaulted(id, error, stack));
                break;

            case nameof(IApplyEndpoints.ApplyValidate):
                await tasks.Validate(
                    (ValidateRequestBase)payload, _unconnected, logStream,
                    id => hub.ApplyValidateCompleted(id),
                    id => hub.ApplyValidateCancelled(id),
                    (id, error, stack) => hub.ApplyValidateFaulted(id, error, stack));
                break;

            // Real: this is what writes the variable files the plan reads, so a canned reply
            // leaves the plan with no values for its variables.
            case nameof(IApplyEndpoints.ApplyVariables):
                await tasks.Variables(
                    (VariablesRequestBase)payload, _unconnected, logStream,
                    (id, set) => hub.ApplyVariablesCompleted(id, set),
                    id => hub.ApplyVariablesCancelled(id),
                    (id, error, stack) => hub.ApplyVariablesFaulted(id, error, stack));
                break;

            case nameof(IApplyEndpoints.ApplyPlan):
                await tasks.Plan(
                    (PlanRequestBase)payload, _unconnected, logStream,
                    (id, data) => hub.ApplyPlanCompleted(id, data),
                    id => hub.ApplyPlanCancelled(id),
                    (id, error, stack, outcome) => hub.ApplyPlanFaulted(id, error, stack, outcome));
                break;

            case nameof(IApplyEndpoints.ApplyFromPlan):
                await tasks.ApplyFromPlan(
                    (ApplyFromPlanRequestBase)payload, _unconnected, logStream,
                    (id, count) => hub.ApplyFromPlanCompleted(id, count),
                    id => hub.ApplyFromPlanCancelled(id),
                    (id, error, stack, count) => hub.ApplyFromPlanFaulted(id, error, stack, count));
                break;

            case nameof(IApplyEndpoints.ApplyProbe):
                await tasks.Probe(
                    (ProbeRequestBase)payload, _unconnected, logStream,
                    (id, signals) => hub.ApplyProbeCompleted(id, signals),
                    id => hub.ApplyProbeCancelled(id),
                    (id, error, stack) => hub.ApplyProbeFaulted(id, error, stack));
                break;

            case nameof(IApplyEndpoints.ApplyOutput):
                await tasks.Output(
                    (OutputRequestBase)payload, _unconnected, logStream,
                    (id, outputs) => hub.ApplyOutputCompleted(id, outputs),
                    id => hub.ApplyOutputCancelled(id),
                    (id, error, stack) => hub.ApplyOutputFaulted(id, error, stack));
                break;

            // Everything else keeps its canned reply: a run exercises one job family's real work,
            // not every family's.
            default:
                await base.Reply(hub, endpoint, payload, jobId);
                break;
        }
    }

    /// <summary>
    /// Copies the fixture Module matching this Module's source subdirectory into the directory the
    /// real GetModule would have cloned it to, which is where init and plan then run.
    /// </summary>
    private void PlaceFixtureModule(GetModuleRequestBase request)
    {
        var name = Path.GetFileName(request.Metadata.SourceSubdirectory?.TrimEnd('/') ?? string.Empty);
        var source = Path.Combine(fixtureRoot, name);

        // An empty name resolves to the fixture root, which exists, so the check below would pass
        // and the run would place nothing while still reporting a healthy apply.
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException(
                $"Module '{request.Metadata.ModuleName}' has no source subdirectory, so there is no " +
                "fixture to place. A semi-fake run plans real code: point --module at one of the " +
                "diamond Modules.");

        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException(
                $"No fixture Module '{name}' under {fixtureRoot}. A semi-fake run plans real code, " +
                "so the Module's source subdirectory has to name one of the fixtures.");

        // The same derivation ModuleDirectoryService makes: the Module's own directory, then its
        // source subdirectory, which is where init and plan run.
        var settings = services.GetRequiredService<IOptions<WorkingDirectorySettings>>().Value;
        var destination = Path.Combine(
            settings.WorkingDirectory,
            request.Metadata.StackName,
            request.Metadata.NamespaceName,
            request.Metadata.ModuleName,
            request.Metadata.SourceSubdirectory ?? string.Empty);

        Directory.CreateDirectory(destination);

        // The engine writes its env file here and does not create the directory itself; the real
        // module getter makes it as part of the checkout.
        Directory.CreateDirectory(Path.Combine(destination, ".snapcd"));

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);

        trace($"  -> placed fixture Module {name} at {destination}");
    }
}
