// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

using ApplyMachine = SnapCd.Server.Core.StateMachine.Jobs.JobStateMachine<
    SnapCd.Server.Core.Entities.Sagas.ApplyJobSaga,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleRequested,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleFailed,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleCompleted,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleCancelled,
    SnapCd.Server.Core.Events.Steps.PlanRequested,
    SnapCd.Server.Core.Events.Steps.PlanCompleted,
    SnapCd.Server.Core.Events.Steps.PlanCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanRequested,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCancelled>;
using DestroyMachine = SnapCd.Server.Core.StateMachine.Jobs.JobStateMachine<
    SnapCd.Server.Core.Entities.Sagas.DestroyJobSaga,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleRequested,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleFailed,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleCompleted,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleCancelled,
    SnapCd.Server.Core.Events.Steps.PlanDestroyRequested,
    SnapCd.Server.Core.Events.Steps.PlanDestroyCompleted,
    SnapCd.Server.Core.Events.Steps.PlanDestroyCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanRequested,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanCancelled>;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// Heartbeat conformance: every reachable saga state must handle or explicitly ignore each of
/// the three heartbeat events, so a tick or an in-flight request response landing just after a
/// state transition can never raise UnhandledEventException. States are enumerated from the
/// state machine itself, so newly added states fail the test until they take a position.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class HeartbeatConformanceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;

    private Module _module = null!;
    private Runner _runner = null!;

    // Finalized terminal states: the saga row is deleted on Finalize, so no event can be
    // delivered to an instance sitting in one of these; heartbeat handling is unreachable there.
    private static readonly string[] FinalizedStates = ["Completed", "Failed", "Cancelled", "Declined", "PolicyDenied"];

    public HeartbeatConformanceTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
        _runner = db.Runners.First(r => r.Id == _module.RunnerId);

        if (!db.RunnerConnections.Any(rc => rc.RunnerId == _runner.Id && rc.InstanceName == "harness"))
        {
            db.RunnerConnections.Add(new RunnerConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = _module.OrganizationId,
                RunnerId = _runner.Id,
                InstanceName = "harness",
                SignalRConnectionId = "harness-connection",
                ServerInstanceId = Guid.NewGuid()
            });
            await db.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ServiceProvider BuildProvider(bool destroy)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_module.OrganizationId]));
        services.AddDistributedMemoryCache();
        services.AddSingleton<SnapCd.Server.Core.Services.MaintenanceMode.IMaintenanceModeService, SnapCd.Server.Core.Services.MaintenanceMode.MaintenanceModeService>();
        services.Configure<ModuleJobRepositorySettings>(_ => { });
        services.AddScoped<ModuleJobRepository>();
        services.AddMassTransitTestHarness(x =>
        {
            if (destroy)
                x.AddSagaStateMachine<DestroyMachine, DestroyJobSaga>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                        r.ExistingDbContext<SnapCdDbContext>();
                    });
            else
                x.AddSagaStateMachine<ApplyMachine, ApplyJobSaga>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                        r.ExistingDbContext<SnapCdDbContext>();
                    });
        });
        return services.BuildServiceProvider(true);
    }

    // Request-pending states ("CancelKillRequested.Pending" etc.) are auto-generated by the
    // Request<> declarations; no behaviour transitions into them, so they are unreachable.
    private static IEnumerable<string> StatesUnderTest(IEnumerable<State> states)
        => states
            .Select(s => s.Name)
            .Where(n => n != "Initial" && n != "Final" && !n.Contains('.'))
            .Except(FinalizedStates);

    private string DeclaredJson()
    {
        var declared = new ResolvedModule
        {
            ModuleId = _module.Id,
            NamespaceId = _module.NamespaceId,
            StackId = _module.Namespace.StackId,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            ModuleName = _module.Name,
            NamespaceName = _module.Namespace.Name,
            StackName = "harness",
            RunnerName = _runner.Name,
            SourceRevision = "main",
            SourceUrl = "https://example.com/repo.git",
            SourceSubdirectory = "",
            Engine = "tofu",
            Policies = new List<ResolvedPolicy>()
        };
        return JsonSerializer.Serialize(declared);
    }

    private async Task<Guid> SeedJob(bool destroy, string state, Guid? heartbeatRequestId = null, int? approvalTimeoutMinutes = null)
    {
        var jobId = Guid.NewGuid();
        await using var db = _fixture.CreateDbContext();

        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _module.OrganizationId,
            ModuleId = _module.Id,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = destroy ? "Destroy" : "Apply",
            IsCurrent = null
        });

        if (destroy)
            db.DestroyJobSagas.Add(new DestroyJobSaga
            {
                CorrelationId = jobId,
                CurrentState = state,
                ModuleId = _module.Id,
                OrganizationId = _module.OrganizationId,
                RunnerId = _runner.Id,
                RunnerName = _runner.Name,
                RunnerInstanceName = "harness",
                DeclaredJson = DeclaredJson(),
                HeartbeatRequestId = heartbeatRequestId,
                ApprovalTimeoutMinutes = approvalTimeoutMinutes
            });
        else
            db.ApplyJobSagas.Add(new ApplyJobSaga
            {
                CorrelationId = jobId,
                CurrentState = state,
                ModuleId = _module.Id,
                OrganizationId = _module.OrganizationId,
                RunnerId = _runner.Id,
                RunnerName = _runner.Name,
                RunnerInstanceName = "harness",
                DeclaredJson = DeclaredJson(),
                HeartbeatRequestId = heartbeatRequestId,
                ApprovalTimeoutMinutes = approvalTimeoutMinutes
            });

        await db.SaveChangesAsync();
        return jobId;
    }

    private static async Task<IReceivedMessage<T>> AwaitConsumed<T>(ITestHarness harness, Guid correlationId)
        where T : StepResponseBase
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var match = harness.Consumed
                .Select<T>(x => ((IReceivedMessage<T>)x).Context.Message.CorrelationId == correlationId)
                .FirstOrDefault();
            if (match != null) return (IReceivedMessage<T>)match;
            await Task.Delay(50);
        }

        throw new TimeoutException($"{typeof(T).Name} for {correlationId} was never consumed");
    }

    private async Task RunTotality(bool destroy)
    {
        var states = destroy
            ? StatesUnderTest(new DestroyMachine(NullLogger<DestroyMachine>.Instance).States)
            : StatesUnderTest(new ApplyMachine(NullLogger<ApplyMachine>.Instance).States);

        var provider = BuildProvider(destroy);
        await using var _ = provider;
        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = TimeSpan.FromSeconds(10);
        await harness.Start();

        var failures = new List<string>();

        foreach (var state in states)
        {
            // Tick
            var tickJob = await SeedJob(destroy, state);
            await harness.Bus.Publish(new HeartbeatScheduled { CorrelationId = tickJob, OrganizationId = _module.OrganizationId });
            var tick = await AwaitConsumed<HeartbeatScheduled>(harness, tickJob);
            if (tick.Exception != null)
                failures.Add($"{state} / HeartbeatScheduled: {tick.Exception.GetType().Name}: {tick.Exception.Message}");

            // In-flight request responses, delivered with the RequestId the saga is holding
            var completedRequestId = Guid.NewGuid();
            var completedJob = await SeedJob(destroy, state, completedRequestId);
            await harness.Bus.Publish(
                new HeartbeatCompleted { CorrelationId = completedJob, OrganizationId = _module.OrganizationId },
                c => c.RequestId = completedRequestId);
            var completed = await AwaitConsumed<HeartbeatCompleted>(harness, completedJob);
            if (completed.Exception != null)
                failures.Add($"{state} / HeartbeatCompleted: {completed.Exception.GetType().Name}: {completed.Exception.Message}");

            var failedRequestId = Guid.NewGuid();
            var failedJob = await SeedJob(destroy, state, failedRequestId);
            await harness.Bus.Publish(
                new HeartbeatFailed { CorrelationId = failedJob, OrganizationId = _module.OrganizationId },
                c => c.RequestId = failedRequestId);
            var failed = await AwaitConsumed<HeartbeatFailed>(harness, failedJob);
            if (failed.Exception != null)
                failures.Add($"{state} / HeartbeatFailed: {failed.Exception.GetType().Name}: {failed.Exception.Message}");
        }

        Assert.True(failures.Count == 0,
            "States raising exceptions on heartbeat events:\n" + string.Join("\n", failures));
    }

    [Fact]
    public Task Apply_Every_State_Handles_Or_Ignores_Heartbeat_Events() => RunTotality(destroy: false);

    [Fact]
    public Task Destroy_Every_State_Handles_Or_Ignores_Heartbeat_Events() => RunTotality(destroy: true);

    [Fact]
    public async Task Approval_Wait_Sets_WaitingSince_And_Approval_Clears_It()
    {
        var provider = BuildProvider(destroy: false);
        await using var _ = provider;
        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = TimeSpan.FromSeconds(10);
        await harness.Start();

        int? originalThreshold;
        await using (var db = _fixture.CreateDbContext())
        {
            var module = db.Modules.First(m => m.Id == _module.Id);
            originalThreshold = module.ApplyApprovalThreshold;
            module.ApplyApprovalThreshold = 1;
            await db.SaveChangesAsync();
        }

        try
        {
            var jobId = await SeedJob(destroy: false, state: "PlanPending");

            await harness.Bus.Publish(new PlanCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, TotalChangedCount = 1 });

            var saga = await WaitForSaga(jobId, s => s.CurrentState == "WaitingForApproval");
            Assert.NotNull(saga);
            Assert.Equal("WaitingForApproval", saga!.CurrentState);
            Assert.NotNull(saga.WaitingSince);

            await using (var db = _fixture.CreateDbContext())
            {
                db.Set<ModuleJobApproval>().Add(new ModuleJobApproval
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _module.OrganizationId,
                    ModuleJobId = jobId,
                    PrincipalId = Guid.NewGuid(),
                    PrincipalDiscriminator = PrincipalDiscriminator.User,
                    DecisionDateTime = DateTime.UtcNow,
                    Declined = false
                });
                await db.SaveChangesAsync();
            }

            await harness.Bus.Publish(new ApprovalReevaluationRequestedEvent { ModuleId = _module.Id, ModuleJobId = jobId });

            saga = await WaitForSaga(jobId, s => s.CurrentState == "ApplyFromPlanPending");
            Assert.NotNull(saga);
            Assert.Equal("ApplyFromPlanPending", saga!.CurrentState);
            Assert.Null(saga.WaitingSince);
        }
        finally
        {
            await using var db = _fixture.CreateDbContext();
            var module = db.Modules.First(m => m.Id == _module.Id);
            module.ApplyApprovalThreshold = originalThreshold;
            await db.SaveChangesAsync();
        }
    }

    private async Task<ApplyJobSaga?> WaitForSaga(Guid jobId, Func<ApplyJobSaga, bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        ApplyJobSaga? saga = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            saga = await db.ApplyJobSagas.AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == jobId);
            if (saga != null && predicate(saga)) return saga;
            await Task.Delay(100);
        }
        return saga;
    }
}
