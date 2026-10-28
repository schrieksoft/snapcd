// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using MassTransit;
using MassTransit.Contracts;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Consumers.Tasks.Handlers;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
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
    SnapCd.Server.Core.Events.Steps.ApplyGetDefinitiveRevisionRequested,
    SnapCd.Server.Core.Events.Steps.ApplyGetDefinitiveRevisionCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyGetDefinitiveRevisionCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyGetDefinitiveRevisionFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyPolicyValidateRequested,
    SnapCd.Server.Core.Events.Steps.ApplyPolicyValidateCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyPolicyValidateCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyPolicyValidateFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyOutputRequested,
    SnapCd.Server.Core.Events.Steps.ApplyOutputCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyOutputCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyOutputFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyGetModuleRequested,
    SnapCd.Server.Core.Events.Steps.ApplyGetModuleCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyGetModuleCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyGetModuleFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyInitRequested,
    SnapCd.Server.Core.Events.Steps.ApplyInitCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyInitCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyInitFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyValidateRequested,
    SnapCd.Server.Core.Events.Steps.ApplyValidateCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyValidateCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyValidateFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyVariablesRequested,
    SnapCd.Server.Core.Events.Steps.ApplyVariablesCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyVariablesCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyVariablesFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyPlanRequested,
    SnapCd.Server.Core.Events.Steps.ApplyPlanCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyPlanCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyPlanFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanRequested,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyProbeRequested,
    SnapCd.Server.Core.Events.Steps.ApplyProbeCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyProbeCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyProbeFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyCancelKillRequested,
    SnapCd.Server.Core.Events.Steps.DummyApplyCancelKillCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyCancelKillCompleted>;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// Server-side failures reach the job wherever the job is: while cancelling, when the cancel is not confirmed, when a
/// step's dispatch or result handling throws, when an output report cannot be matched, and when a runner reports late.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ServerSideJobErrorTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Module _module = null!;
    private Runner _runner = null!;

    public ServerSideJobErrorTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
            _runner = db.Runners.First(r => r.Id == _module.RunnerId);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_module.OrganizationId]));
        services.AddDistributedMemoryCache();
        services.AddSingleton<SnapCd.Server.Core.Services.MaintenanceMode.IMaintenanceModeService, SnapCd.Server.Core.Services.MaintenanceMode.MaintenanceModeService>();
        services.Configure<ModuleJobRepositorySettings>(_ => { });
        services.AddScoped<ModuleJobRepository>();

        // The output store is not reached on the paths under test.
        services.AddScoped(sp => new ApplyOutputCompletedInvokedConsumer(
            sp.GetRequiredService<ILogger<ApplyOutputCompletedInvokedConsumer>>(),
            sp.GetRequiredService<SnapCdDbContext>(), null!, sp.GetRequiredService<IBus>()));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<ApplyOutputCompletedInvokedConsumer>();
            x.AddSagaStateMachine<ApplyMachine, ApplyJobSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(10);
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
    }

    private async Task<Guid> SeedJob(string state, Guid? heartbeatRequestId = null, Guid? killRequestId = null)
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
            JobType = "Apply",
            IsCurrent = null
        });
        db.ApplyJobSagas.Add(new ApplyJobSaga
        {
            CorrelationId = jobId,
            CurrentState = state,
            ModuleId = _module.Id,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            RunnerName = _runner.Name,
            RunnerInstanceName = "harness",
            HeartbeatRequestId = heartbeatRequestId,
            KillCancellationRequestId = killRequestId,
            DeclaredJson = JsonSerializer.Serialize(new ResolvedModule
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
                Engine = "tofu"
            })
        });

        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task<ModuleJob> WaitForEnd(Guid jobId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            await using var db = _fixture.CreateDbContext();
            var job = await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId && j.OrganizationId == _module.OrganizationId);
            if (job.Status != ExecutionStatus.Running || DateTime.UtcNow >= deadline) return job;
            await Task.Delay(100);
        }
    }

    private Task PublishFault<T>(T message) where T : class =>
        _harness.Bus.Publish<Fault<T>>(new MassTransit.Events.FaultEvent<T>(
            message, Guid.NewGuid(), MassTransit.Metadata.HostMetadataCache.Host, new InvalidOperationException("dispatch blew up"),
            [$"urn:message:{typeof(T).FullName}"]));

    [Fact]
    public async Task Server_Fault_While_Cancelling_After_Current_Cancels_And_Keeps_The_Error()
    {
        var jobId = await SeedJob("CancellingAfterCurrent");

        await _harness.Bus.Publish(new ApplyInitFaulted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, ErrorMessage = "init dispatch failed", IsServerSideError = true });

        var job = await WaitForEnd(jobId);
        Assert.Equal(ExecutionStatus.Cancelled, job.Status);
        Assert.Equal(ServerSideStep.Init, job.FailedOnServerSideStep);
        Assert.Equal("Server error during Init while cancelling", job.ServerSideErrorHeader);
        Assert.Contains("init dispatch failed", job.ServerSideError);
    }

    [Fact]
    public async Task Runner_Fault_While_Cancelling_After_Current_Cancels_Without_A_Server_Error()
    {
        var jobId = await SeedJob("CancellingAfterCurrent");

        await _harness.Bus.Publish(new ApplyInitFaulted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, ErrorMessage = "tofu init failed" });

        var job = await WaitForEnd(jobId);
        Assert.Equal(ExecutionStatus.Cancelled, job.Status);
        Assert.Null(job.ServerSideErrorHeader);
    }

    [Fact]
    public async Task Step_Cancel_Report_While_Cancelling_After_Current_Cancels()
    {
        var jobId = await SeedJob("CancellingAfterCurrent");

        await _harness.Bus.Publish(new ApplyInitCancelled { CorrelationId = jobId, OrganizationId = _module.OrganizationId });

        var job = await WaitForEnd(jobId);
        Assert.Equal(ExecutionStatus.Cancelled, job.Status);
    }

    [Fact]
    public async Task Lost_Runner_While_Cancelling_After_Current_Cancels_With_The_Reason()
    {
        var heartbeatRequestId = Guid.NewGuid();
        var jobId = await SeedJob("CancellingAfterCurrent", heartbeatRequestId: heartbeatRequestId);

        await _harness.Bus.Publish(
            new HeartbeatFailed { CorrelationId = jobId, OrganizationId = _module.OrganizationId, Reason = "Runner instance 'harness' is not connected." },
            c => c.RequestId = heartbeatRequestId);

        var job = await WaitForEnd(jobId);
        Assert.Equal(ExecutionStatus.Cancelled, job.Status);
        Assert.Equal("Runner stopped responding", job.ServerSideErrorHeader);
        Assert.Contains("Runner instance 'harness' is not connected.", job.ServerSideError);
    }

    [Fact]
    public async Task Unconfirmed_Cancel_Kill_Closes_The_Job_As_Cancelled_With_A_Warning()
    {
        var killRequestId = Guid.NewGuid();
        var jobId = await SeedJob("CancellingImmediateKill", killRequestId: killRequestId);

        await _harness.Bus.Publish<RequestTimeoutExpired<ApplyCancelKillRequested>>(new
        {
            CorrelationId = jobId,
            RequestId = killRequestId,
            Timestamp = DateTime.UtcNow,
            ExpirationTime = DateTime.UtcNow
        });

        var job = await WaitForEnd(jobId);
        Assert.Equal(ExecutionStatus.Cancelled, job.Status);
        Assert.Equal("Runner did not confirm the cancel", job.ServerSideErrorHeader);
    }

    [Fact]
    public async Task Fault_Of_A_Step_Request_Fails_The_Job_With_The_Exception()
    {
        var jobId = await SeedJob("InitPending");

        await PublishFault(new ApplyInitRequested { CorrelationId = jobId, OrganizationId = _module.OrganizationId });

        var job = await WaitForEnd(jobId);
        Assert.Equal(ExecutionStatus.Failed, job.Status);
        Assert.Equal(ServerSideStep.Init, job.FailedOnServerSideStep);
        Assert.Equal("Server error while handling ApplyInitRequested", job.ServerSideErrorHeader);
        Assert.Contains("dispatch blew up", job.ServerSideError);
    }

    [Fact]
    public async Task Fault_Of_Handling_A_Step_Result_Fails_The_Job_With_The_Exception()
    {
        var jobId = await SeedJob("InitPending");

        await PublishFault(new ApplyInitCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId });

        var job = await WaitForEnd(jobId);
        Assert.Equal(ExecutionStatus.Failed, job.Status);
        Assert.Equal("Server error while handling ApplyInitCompleted", job.ServerSideErrorHeader);
        Assert.Contains("dispatch blew up", job.ServerSideError);
    }

    [Fact]
    public async Task Empty_Output_Report_Completes_The_Output_Step()
    {
        var jobId = Guid.NewGuid();

        await _harness.Bus.Publish(new ApplyOutputCompletedInvoked { JobId = jobId, OutputSet = null });

        Assert.True(await _harness.Published.Any<ApplyOutputCompleted>(x => x.Context.Message.CorrelationId == jobId));
    }

    [Fact]
    public async Task Output_Report_For_An_Unknown_Job_Faults_The_Step_As_Server_Side()
    {
        var jobId = Guid.NewGuid();

        await _harness.Bus.Publish(new ApplyOutputCompletedInvoked
        {
            JobId = jobId,
            OutputSet = new OutputSetCreateDto { Checksum = Guid.NewGuid().ToString("N"), Timestamp = 1 }
        });

        Assert.True(await _harness.Published.Any<ApplyOutputFaulted>(x =>
            x.Context.Message.CorrelationId == jobId && x.Context.Message.IsServerSideError));
    }

    [Fact]
    public async Task Late_Report_Is_Noted_Once_And_Only_For_The_Modules_Runner()
    {
        var jobId = Guid.NewGuid();
        await using (var db = _fixture.CreateDbContext())
        {
            db.ModuleJobs.Add(new ModuleJob
            {
                Id = jobId,
                OrganizationId = _module.OrganizationId,
                ModuleId = _module.Id,
                TimestampStart = DateTimeOffset.UtcNow,
                TimestampEnd = DateTimeOffset.UtcNow,
                Status = ExecutionStatus.Failed,
                JobType = "Apply",
                IsCurrent = false
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateDbContext())
            await RunnerJobAuthorizationService.RecordLateReport(db, jobId, _module.OrganizationId, Guid.NewGuid());
        await using (var db = _fixture.CreateDbContext())
            Assert.Null((await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId)).ServerSideError);

        for (var i = 0; i < 2; i++)
        {
            await using var db = _fixture.CreateDbContext();
            await RunnerJobAuthorizationService.RecordLateReport(db, jobId, _module.OrganizationId, _runner.Id);
        }

        await using (var db = _fixture.CreateDbContext())
        {
            var job = await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
            Assert.Equal("The runner reported on this job after the server had ended it", job.ServerSideErrorHeader);
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(job.ServerSideError!, "reported on this job after the server had ended it"));
        }
    }

    [Fact]
    public async Task Failed_Liveness_Recording_Is_Rethrown_For_Retry()
    {
        var consumer = new ReportRunningTaskInvokedConsumer(
            NullLogger<ReportRunningTaskInvokedConsumer>.Instance, null!, null!, new UnavailableDbContext());
        var context = new Moq.Mock<ConsumeContext<ReportRunningTaskInvoked>>();
        context.SetupGet(c => c.Message).Returns(new ReportRunningTaskInvoked { JobId = Guid.NewGuid(), OrganizationId = _module.OrganizationId, RunnerId = _runner.Id, TaskName = "Plan" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(context.Object));
    }

    /// Stands in for a database that cannot be reached: the first query throws.
    private sealed class UnavailableDbContext() : SnapCdDbContext(
        new DbContextOptionsBuilder<SnapCdDbContext>()
            .UseSqlServer("Server=unused", o => o.ExecutionStrategy(_ => new ThrowingStrategy()))
            .Options)
    {
        public const string Message = "database unavailable";

        private sealed class ThrowingStrategy : Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy
        {
            public bool RetriesOnFailure => false;

            public TResult Execute<TState, TResult>(
                TState state,
                Func<DbContext, TState, TResult> operation,
                Func<DbContext, TState, Microsoft.EntityFrameworkCore.Storage.ExecutionResult<TResult>>? verifySucceeded)
                => throw new InvalidOperationException(Message);

            public Task<TResult> ExecuteAsync<TState, TResult>(
                TState state,
                Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
                Func<DbContext, TState, CancellationToken, Task<Microsoft.EntityFrameworkCore.Storage.ExecutionResult<TResult>>>? verifySucceeded,
                CancellationToken cancellationToken = default)
                => throw new InvalidOperationException(Message);
        }
    }
}
