// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.StateMachine.SplitMonolith;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.SplitMonolith;

/// <summary>
/// The proof is where the two split jobs part: a prove job ends there, a migrate job goes on to
/// approval. Same saga, same events, one flag.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class SplitProveStateMachineTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private Guid _moduleId;
    private Guid _organizationId;
    private readonly List<Guid> _seeded = [];

    public SplitProveStateMachineTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IMaintenanceModeService, MaintenanceModeService>();
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_organizationId]));
        services.AddScoped<ManualModuleJobRepository>();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<SplitMonolithStateMachine, SplitMonolithSaga>()
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

        await using var db = _fixture.CreateDbContext();
        await db.Set<SplitMonolithSaga>().Where(s => _seeded.Contains(s.CorrelationId)).ExecuteDeleteAsync();
        await db.ManualModuleJobs.Where(j => _seeded.Contains(j.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Prove_Job_Ends_After_The_Proof()
    {
        var jobId = await Seed(stopAfterProve: true);

        await _harness.Bus.Publish(new MigrateProveCompleted { CorrelationId = jobId, OrganizationId = _organizationId, ModulesProven = 3, ModulesPlanningClean = 3 });

        Assert.True(await WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            return db.ManualModuleJobs.AsNoTracking().Single(j => j.Id == jobId).Status == ExecutionStatus.Completed;
        }));
        Assert.True(_harness.Published.Select<SplitMonolithCompleted>().Any(m => m.Context.Message.ModuleJobId == jobId));
        await using var check = _fixture.CreateDbContext();
        var job = await check.ManualModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
        Assert.NotNull(job.TimestampEnd);
        Assert.NotEqual(true, job.WaitingForApproval);
        Assert.True(await WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            return !db.Set<SplitMonolithSaga>().Any(s => s.CorrelationId == jobId);
        }));
    }

    [Fact]
    public async Task A_Migrate_Job_Waits_For_Approval_After_The_Proof()
    {
        var jobId = await Seed(stopAfterProve: false);

        await _harness.Bus.Publish(new MigrateProveCompleted { CorrelationId = jobId, OrganizationId = _organizationId, ModulesProven = 3, ModulesPlanningClean = 3 });

        Assert.True(await WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            return db.Set<SplitMonolithSaga>().AsNoTracking().SingleOrDefault(s => s.CorrelationId == jobId)?.CurrentState == "WaitingForApproval";
        }));
        await using var check = _fixture.CreateDbContext();
        var job = await check.ManualModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
        Assert.Equal(ExecutionStatus.Running, job.Status);
        Assert.True(job.WaitingForApproval);
        Assert.Empty(_harness.Published.Select<SplitMonolithCompleted>().Where(m => m.Context.Message.ModuleJobId == jobId));
    }

    [Theory]
    [InlineData("PlanEmptyVerifyPending")]
    [InlineData("RefactorDiffPending")]
    [InlineData("MigrateProvePending")]
    public async Task A_Faulted_Step_Fails_The_Job(string state)
    {
        var jobId = await Seed(stopAfterProve: true, state: state);

        await PublishFaultFor(state, jobId);

        Assert.True(await WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            return db.ManualModuleJobs.AsNoTracking().Single(j => j.Id == jobId).Status == ExecutionStatus.Failed;
        }), $"the job was not failed from {state}");
        Assert.True(_harness.Published.Select<SplitMonolithFailed>().Any(m => m.Context.Message.ModuleJobId == jobId));
    }

    [Theory]
    [InlineData("PlanEmptyVerifyPending")]
    [InlineData("RefactorDiffPending")]
    [InlineData("MigrateProvePending")]
    public async Task A_Cancelled_Step_Cancels_The_Job(string state)
    {
        var jobId = await Seed(stopAfterProve: true, state: state);

        await PublishCancelFor(state, jobId);

        Assert.True(await WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            return db.ManualModuleJobs.AsNoTracking().Single(j => j.Id == jobId).Status == ExecutionStatus.Cancelled;
        }), $"the job was not cancelled from {state}");
        Assert.True(_harness.Published.Select<SplitMonolithCancelled>().Any(m => m.Context.Message.ModuleJobId == jobId));
    }

    /// <summary>A server-side fault records which step broke, so the failure names itself on the job page.</summary>
    [Fact]
    public async Task A_Server_Side_Fault_Records_The_Step_And_The_Error()
    {
        var jobId = await Seed(stopAfterProve: true, state: "MigrateProvePending");

        await _harness.Bus.Publish(new MigrateProveFaulted
        {
            CorrelationId = jobId,
            OrganizationId = _organizationId,
            IsServerSideError = true,
            ErrorMessage = "the runner went away",
            StackTrace = "at Somewhere.Deep()"
        });

        Assert.True(await WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            return db.ManualModuleJobs.AsNoTracking().Single(j => j.Id == jobId).Status == ExecutionStatus.Failed;
        }));
        await using var check = _fixture.CreateDbContext();
        var job = await check.ManualModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
        Assert.Contains("the runner went away", job.ServerSideError);
        Assert.NotNull(job.ServerSideErrorHeader);
    }

    /// <summary>
    /// A cancel arriving when the job is already over. Nothing to do, but it must not fault: an
    /// unhandled event is retried and then dead-lettered, which looks like a broken cancel.
    /// </summary>
    [Theory]
    [InlineData("Completed")]
    [InlineData("Failed")]
    [InlineData("Cancelled")]
    public async Task A_Cancel_On_A_Finished_Job_Is_Absorbed(string state)
    {
        var jobId = await Seed(stopAfterProve: true, state: state);

        await PublishCancel(jobId);

        Assert.Null(await ConsumedCancel(jobId));
    }

    /// <summary>
    /// Cancelling again while the timeout is still due does nothing: re-issuing would restart the
    /// clock, so repeated clicks would push the deadline out indefinitely.
    /// </summary>
    [Theory]
    [InlineData("CancellingImmediateKill")]
    [InlineData("CancellingImmediateGraceful")]
    [InlineData("CancellingAfterCurrent")]
    public async Task Cancelling_Again_Inside_The_Window_Changes_Nothing(string state)
    {
        var jobId = await Seed(stopAfterProve: true, state: state, waitingSince: DateTime.UtcNow.AddSeconds(-5));

        await PublishCancel(jobId);

        Assert.Null(await ConsumedCancel(jobId));
        await using var db = _fixture.CreateDbContext();
        Assert.Equal(ExecutionStatus.Running, (await db.ManualModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId)).Status);
        Assert.Equal(state, db.Set<SplitMonolithSaga>().AsNoTracking().Single(s => s.CorrelationId == jobId).CurrentState);
    }

    /// <summary>
    /// Cancelling again once the timeout was due means the timeout was lost: nothing else will ever
    /// end this job, so the click forces it closed.
    /// </summary>
    [Theory]
    [InlineData("CancellingImmediateKill")]
    [InlineData("CancellingImmediateGraceful")]
    [InlineData("CancellingAfterCurrent")]
    public async Task Cancelling_Again_After_The_Timeout_Was_Due_Forces_It_Closed(string state)
    {
        var jobId = await Seed(stopAfterProve: true, state: state, waitingSince: DateTime.UtcNow.AddMinutes(-10));

        await PublishCancel(jobId);

        Assert.True(await WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            return db.ManualModuleJobs.AsNoTracking().Single(j => j.Id == jobId).Status == ExecutionStatus.Cancelled;
        }), "the stuck job was not forced closed");
        Assert.True(_harness.Published.Select<SplitMonolithCancelled>().Any(m => m.Context.Message.ModuleJobId == jobId));
    }

    private Task PublishCancel(Guid jobId) => _harness.Bus.Publish(new CancelManualModuleJobRequested
    {
        CorrelationId = jobId,
        OrganizationId = _organizationId,
        CancellationType = CancellationType.ImmediateKill
    });

    /// <summary>The consumer's exception, or null when it handled the message cleanly.</summary>
    private async Task<Exception?> ConsumedCancel(Guid jobId)
    {
        Assert.True(await WaitUntil(() =>
            _harness.Consumed.Select<CancelManualModuleJobRequested>().Any(m => m.Context.Message.CorrelationId == jobId)),
            "the cancel was never consumed");

        return _harness.Consumed.Select<CancelManualModuleJobRequested>().First(m => m.Context.Message.CorrelationId == jobId).Exception;
    }

    private Task PublishFaultFor(string state, Guid jobId) => state switch
    {
        "PlanEmptyVerifyPending" => _harness.Bus.Publish(new PlanEmptyVerifyFaulted { CorrelationId = jobId, OrganizationId = _organizationId, ErrorMessage = "not empty" }),
        "RefactorDiffPending" => _harness.Bus.Publish(new RefactorDiffFaulted { CorrelationId = jobId, OrganizationId = _organizationId, ErrorMessage = "committed roots differ from the map" }),
        _ => _harness.Bus.Publish(new MigrateProveFaulted { CorrelationId = jobId, OrganizationId = _organizationId, ErrorMessage = "a carved module did not plan clean" })
    };

    private Task PublishCancelFor(string state, Guid jobId) => state switch
    {
        "PlanEmptyVerifyPending" => _harness.Bus.Publish(new PlanEmptyVerifyCancelled { CorrelationId = jobId, OrganizationId = _organizationId }),
        "RefactorDiffPending" => _harness.Bus.Publish(new RefactorDiffCancelled { CorrelationId = jobId, OrganizationId = _organizationId }),
        _ => _harness.Bus.Publish(new MigrateProveCancelled { CorrelationId = jobId, OrganizationId = _organizationId })
    };

    private async Task<Guid> Seed(bool stopAfterProve, string state = "MigrateProvePending", DateTime? waitingSince = null)
    {
        var jobId = Guid.NewGuid();
        _seeded.Add(jobId);
        await using var db = _fixture.CreateDbContext();
        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = stopAfterProve ? ManualJobTypes.SplitProve : ManualJobTypes.SplitMonolith,
            Status = ExecutionStatus.Running
        });
        db.Set<SplitMonolithSaga>().Add(new SplitMonolithSaga
        {
            CorrelationId = jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            CurrentState = state,
            RunnerId = _fixture.Runners["0"].Id,
            RunnerName = "runner",
            RunnerInstanceName = "prove-test",
            DeclaredJson = "{}",
            StopAfterProve = stopAfterProve,
            WaitingSince = waitingSince,
            ApprovalTimeoutMinutes = 0,
            RowVersion = []
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private static async Task<bool> WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            await Task.Delay(100);
        }

        return condition();
    }
}
