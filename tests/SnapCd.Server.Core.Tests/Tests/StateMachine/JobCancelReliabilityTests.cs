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
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
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

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// Cancel must never be a dead end. A click while the timeout is still due does nothing; a click
/// once it was due means the timeout was lost, and forces the job closed. Neither may fault: an
/// unhandled event is retried and then dead-lettered, which the operator sees as a broken button.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class JobCancelReliabilityTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private Module _module = null!;
    private Runner _runner = null!;
    private readonly List<Guid> _seeded = [];

    public JobCancelReliabilityTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
            _runner = db.Runners.First(r => r.Id == _module.RunnerId);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_module.OrganizationId]));
        services.Configure<ModuleJobRepositorySettings>(_ => { });
        services.AddScoped<ModuleJobRepository>();
        services.AddMassTransitTestHarness(x =>
        {
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

        await using var db = _fixture.CreateDbContext();
        await db.ApplyJobSagas.Where(s => _seeded.Contains(s.CorrelationId)).ExecuteDeleteAsync();
        await db.ModuleJobs.Where(j => _seeded.Contains(j.Id)).ExecuteDeleteAsync();
    }

    /// <summary>
    /// CancellingAfterCurrent is excluded: nothing has been sent to the runner there, so a repeat
    /// legitimately escalates to an immediate cancel.
    /// </summary>
    [Theory]
    [InlineData("CancellingImmediateKill")]
    [InlineData("CancellingImmediateGraceful")]
    public async Task Cancelling_Again_Inside_The_Window_Changes_Nothing(string state)
    {
        var jobId = await Seed(state, DateTime.UtcNow.AddSeconds(-5));

        await PublishCancel(jobId, ModeOf(state));

        Assert.Null(await ConsumedCancel(jobId));
        await using var db = _fixture.CreateDbContext();
        Assert.Equal(ExecutionStatus.Running, (await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId)).Status);
        Assert.Equal(state, db.ApplyJobSagas.AsNoTracking().Single(s => s.CorrelationId == jobId).CurrentState);
    }

    [Theory]
    [InlineData("CancellingImmediateKill")]
    [InlineData("CancellingImmediateGraceful")]
    public async Task Cancelling_Again_After_The_Timeout_Was_Due_Forces_It_Closed(string state)
    {
        var jobId = await Seed(state, DateTime.UtcNow.AddMinutes(-10));

        await PublishCancel(jobId, ModeOf(state));

        Assert.True(await WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            return db.ModuleJobs.AsNoTracking().Single(j => j.Id == jobId).Status == ExecutionStatus.Cancelled;
        }), "the stuck job was not forced closed");
        Assert.True(_harness.Published.Select<ApplyModuleCancelled>().Any(m => m.Context.Message.ModuleJobId == jobId));
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Failed")]
    [InlineData("Cancelled")]
    public async Task A_Cancel_On_A_Finished_Job_Is_Absorbed(string state)
    {
        var jobId = await Seed(state, waitingSince: null);

        await PublishCancel(jobId);

        Assert.Null(await ConsumedCancel(jobId));
    }

    /// <summary>
    /// A cancel is published, so every job saga sees every cancel. An id belonging to another kind
    /// of job is not this saga's to finalize, and must be ignored rather than faulting: the repository
    /// throws when the id is not a ModuleJob.
    /// </summary>
    [Fact]
    public async Task A_Cancel_For_Another_Kind_Of_Job_Is_Ignored()
    {
        var foreignId = Guid.NewGuid();

        await _harness.Bus.Publish(new CancelModuleRequested
        {
            CorrelationId = foreignId,
            OrganizationId = _module.OrganizationId,
            CancellationType = CancellationType.ImmediateKill
        });

        Assert.True(await WaitUntil(() =>
            _harness.Consumed.Select<CancelModuleRequested>().Any(m => m.Context.Message.CorrelationId == foreignId)),
            "the cancel was never consumed");

        var consumed = _harness.Consumed.Select<CancelModuleRequested>().First(m => m.Context.Message.CorrelationId == foreignId);
        Assert.Null(consumed.Exception);
    }

    /// <summary>The mode already in flight in that state, so the cancel is a repeat and not an escalation.</summary>
    private static CancellationType ModeOf(string state) => state switch
    {
        "CancellingImmediateGraceful" => CancellationType.ImmediateGraceful,
        _ => CancellationType.ImmediateKill
    };

    private Task PublishCancel(Guid jobId, CancellationType mode = CancellationType.ImmediateKill) =>
        _harness.Bus.Publish(new CancelModuleRequested
        {
            CorrelationId = jobId,
            OrganizationId = _module.OrganizationId,
            CancellationType = mode
        });

    private async Task<Exception?> ConsumedCancel(Guid jobId)
    {
        Assert.True(await WaitUntil(() =>
            _harness.Consumed.Select<CancelModuleRequested>().Any(m => m.Context.Message.CorrelationId == jobId)),
            "the cancel was never consumed");

        return _harness.Consumed.Select<CancelModuleRequested>().First(m => m.Context.Message.CorrelationId == jobId).Exception;
    }

    private async Task<Guid> Seed(string state, DateTime? waitingSince)
    {
        var jobId = Guid.NewGuid();
        _seeded.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _module.OrganizationId,
            ModuleId = _module.Id,
            TimestampStart = DateTimeOffset.UtcNow.AddMinutes(-20),
            Status = ExecutionStatus.Running,
            JobType = "Apply",
            IsCurrent = null
        });
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
            Policies = []
        };
        db.ApplyJobSagas.Add(new ApplyJobSaga
        {
            CorrelationId = jobId,
            CurrentState = state,
            ModuleId = _module.Id,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            RunnerName = _runner.Name,
            RunnerInstanceName = "cancel-harness",
            DeclaredJson = JsonSerializer.Serialize(declared),
            WaitingSince = waitingSince
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
