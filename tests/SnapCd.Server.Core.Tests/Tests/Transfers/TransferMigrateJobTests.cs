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
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.StateMachine.ManualJobs.Activities;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;
using SnapCd.Server.Core.StateMachine.Transfers.Migrate;
using SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Transfers;

/// <summary>
/// One Module's state move, which is what a transfer is two of. The job runs its own sequence and
/// ends: nothing coordinates it, and nothing is written against a prove that came back red.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransferMigrateJobTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Guid _moduleId;
    private Guid _organizationId;
    private Guid _transferId;
    private Guid _jobId;

    public TransferMigrateJobTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        _transferId = Guid.NewGuid();
        _jobId = Guid.NewGuid();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddSingleton<IStateEncryptionService>(new StateEncryptionService(
            Options.Create(new StateStoreSettings { EncryptionKey = Convert.ToBase64String(new byte[32]) })));
        services.AddScoped<IMaintenanceModeService, MaintenanceModeService>();
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_organizationId]));
        services.AddScoped<ManualModuleJobRepository>();
        services.AddScoped<ManualJobStepService>();
        // MassTransit resolves activities from the container, and nothing registers them by
        // convention; the approval gate needs its three.
        services.AddScoped(typeof(TransferMigrateNeedsApprovalActivity<>));
        services.AddScoped(typeof(WaitingForApprovalManualJobActivity<,>));
        services.AddScoped(typeof(NotWaitingForApprovalManualJobActivity<,>));
        services.AddScoped(typeof(CancelManualModuleJobActivity<,>));
        services.AddScoped<TransferArtefactService>();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<TransferMigrateStateMachine, TransferMigrateSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(60);
        await _harness.Start();

        await using var db = _fixture.CreateDbContext();
        (await db.Modules.SingleAsync(m => m.Id == _moduleId)).StateMigrationApprovalThreshold = 1;

        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = _jobId,
            ModuleId = _moduleId,
            TransferId = _transferId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = ManualJobTypes.TransferMigrate,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();

        await using var db = _fixture.CreateDbContext();
        await db.Set<TransferMigrateSaga>().Where(s => s.TransferId == _transferId).ExecuteDeleteAsync();
        (await db.Modules.SingleAsync(m => m.Id == _moduleId)).StateMigrationApprovalThreshold = null;
        await db.SaveChangesAsync();

        await db.ManualModuleJobApprovals.Where(a => a.ManualModuleJobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobSteps.Where(s => s.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobs.Where(j => j.Id == _jobId).ExecuteDeleteAsync();
    }

    /// <summary>
    /// A clean prove reaches the approval gate and stops there: the write is the one irreversible
    /// step, so nothing is dispatched until it is approved.
    /// </summary>
    [Fact]
    public async Task A_Clean_Prove_Waits_For_Approval()
    {
        await Start();
        await Preamble();

        var map = await AwaitStep<TransferMigrateMapRequested>("MigrateMapPending");
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = map.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            FragmentState = "{\"serial\":4}",
            NeedsValuesFrom = []
        });

        var prove = await AwaitStep<TransferMigrateProveRequested>("MigrateProvePending");
        await Answer(new TransferMigrateProveCompleted
        {
            CorrelationId = prove.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            ExitCode = 0
        });

        // The write is gated: nothing is dispatched until the threshold is met.
        Assert.True(await WaitUntil(() => InState("WaitingForApproval")), "the job did not wait for approval");
        Assert.Empty(_harness.Published.Select<TransferMigrateRunRequested>());

    }

    /// <summary>A decline ends the job with nothing written.</summary>
    [Fact]
    public async Task A_Declined_Approval_Writes_Nothing()
    {
        await Start();
        await Preamble();

        var map = await AwaitStep<TransferMigrateMapRequested>("MigrateMapPending");
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = map.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            NeedsValuesFrom = []
        });

        var prove = await AwaitStep<TransferMigrateProveRequested>("MigrateProvePending");
        await Answer(new TransferMigrateProveCompleted
        {
            CorrelationId = prove.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            ExitCode = 0
        });

        Assert.True(await WaitUntil(() => InState("WaitingForApproval")), "the job did not wait for approval");

        await Approve(declined: true);

        Assert.True(await WaitUntil(() => InState("Failed") || SagaIsGone()),
            $"the job did not end; it is in {CurrentState()}");
        Assert.Empty(_harness.Published.Select<TransferMigrateRunRequested>());
    }

    /// <summary>Nothing is written against a prove that came back red.</summary>
    [Fact]
    public async Task A_Refused_Prove_Writes_Nothing()
    {
        await Start();
        await Preamble();

        var map = await AwaitStep<TransferMigrateMapRequested>("MigrateMapPending");
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = map.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            NeedsValuesFrom = []
        });

        var prove = await AwaitStep<TransferMigrateProveRequested>("MigrateProvePending");
        await Answer(new TransferMigrateProveCompleted
        {
            CorrelationId = prove.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            ExitCode = 2,
            Verdict = "one resource would be created"
        });

        Assert.True(await WaitUntil(SagaIsGone), "the job did not end");
        Assert.Empty(_harness.Published.Select<TransferMigrateRunRequested>());

        // The job row carries the outcome: it is what the Module's page reads.
        Assert.True(await WaitUntil(JobHasEnded), "the job row was left Running");
    }

    /// <summary>A dirty plan ends the job before any state is touched.</summary>
    [Fact]
    public async Task A_Dirty_Plan_Ends_The_Job_Before_The_State_Is_Pulled()
    {
        await Start();
        await Preamble(totalChangedCount: 3);

        Assert.True(await WaitUntil(SagaIsGone), "the job did not end");
        Assert.Empty(_harness.Published.Select<TransferMigrateMapRequested>());
    }

    private async Task Start() =>
        await _harness.Bus.Publish(new TransferMigrateRequested
        {
            CorrelationId = _jobId,
            TransferId = _transferId,
            Role = TransferRole.Source,
            Declared = Declared(),
            ProveRef = "main"
        });

    /// <summary>Answers the four steps every job runs before it touches state.</summary>
    private async Task Preamble(int totalChangedCount = 0)
    {
        var select = await AwaitStep<TransferSelectRunnerInstanceRequested>("SelectRunnerInstancePending");
        await Answer(new TransferSelectRunnerInstanceCompleted
        {
            CorrelationId = select.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            RunnerInstanceName = "instance-1"
        });

        var get = await AwaitStep<TransferGetModuleRequested>("GetModulePending");
        Assert.Equal("main", get.SourceRevisionOverride);
        await Answer(new TransferGetModuleCompleted
        {
            CorrelationId = get.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            DefinitiveRevision = "abc123"
        });

        var init = await AwaitStep<TransferInitRequested>("InitPending");
        await Answer(new TransferInitCompleted
        {
            CorrelationId = init.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId
        });

        var validate = await AwaitStep<TransferValidateRequested>("ValidatePending");
        await Answer(new TransferValidateCompleted
        {
            CorrelationId = validate.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId
        });

        var plan = await AwaitStep<TransferPlanRequested>("PlanPending");
        await Answer(new TransferPlanCompleted
        {
            CorrelationId = plan.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            TotalChangedCount = totalChangedCount
        });
    }

    /// <summary>
    /// The request the runner would pick up, once the saga has settled in the state that accepts
    /// the reply: a request is published from inside the handler, before its transition commits.
    /// </summary>
    private async Task<T> AwaitStep<T>(string expectedState) where T : TransferStepRequestBase
    {
        Assert.True(
            await WaitUntil(() => _harness.Published.Select<T>().Any()),
            $"{typeof(T).Name} was never asked for");

        Assert.True(
            await WaitUntil(() =>
            {
                using var db = _fixture.CreateDbContext();
                return db.Set<TransferMigrateSaga>().AsNoTracking()
                    .Any(x => x.CorrelationId == _jobId && x.CurrentState == expectedState);
            }),
            $"the job never settled in {expectedState}; it is in {CurrentState()}");

        return _harness.Published.Select<T>().First().Context.Message;
    }

    private Task Answer<T>(T message) where T : class => _harness.Bus.Publish(message);

    /// <summary>Records a decision and tells the saga to look again, as the approvals API does.</summary>
    private async Task Approve(bool declined = false)
    {
        await using (var db = _fixture.CreateDbContext())
        {
            // Pinned here rather than at setup: the Modules row is shared with every other test in
            // the collection, so a neighbour's cleanup can reset it between the two.
            (await db.Modules.SingleAsync(m => m.Id == _moduleId)).StateMigrationApprovalThreshold = 1;

            db.ManualModuleJobApprovals.Add(new ManualModuleJobApproval
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ManualModuleJobId = _jobId,
                PrincipalId = Guid.NewGuid(),
                PrincipalDiscriminator = PrincipalDiscriminator.User,
                DecisionDateTime = DateTime.UtcNow,
                Declined = declined
            });
            await db.SaveChangesAsync();
        }

        await _harness.Bus.Publish(new ApprovalReevaluationRequestedEvent
        {
            ModuleId = _moduleId,
            ModuleJobId = _jobId
        });
    }

    private string CurrentState()
    {
        using var db = _fixture.CreateDbContext();
        return db.Set<TransferMigrateSaga>().AsNoTracking()
            .Where(x => x.CorrelationId == _jobId)
            .Select(x => x.CurrentState)
            .FirstOrDefault() ?? "(gone)";
    }

    private bool InState(string state)
    {
        using var db = _fixture.CreateDbContext();
        return db.Set<TransferMigrateSaga>().AsNoTracking()
            .Any(x => x.CorrelationId == _jobId && x.CurrentState == state);
    }

    /// <summary>Whether the job row itself was closed out, not just the saga.</summary>
    private bool JobHasEnded()
    {
        using var db = _fixture.CreateDbContext();
        return db.ManualModuleJobs.AsNoTracking()
            .Any(j => j.Id == _jobId && j.Status != ExecutionStatus.Running);
    }

    /// <summary>Finalized: the saga is the job, so it is removed when the job ends.</summary>
    private bool SagaIsGone()
    {
        using var db = _fixture.CreateDbContext();
        return !db.Set<TransferMigrateSaga>().AsNoTracking().Any(x => x.CorrelationId == _jobId);
    }

    private static async Task<bool> WaitUntil(Func<bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate()) return true;
            await Task.Delay(100);
        }

        return predicate();
    }

    private ResolvedModule Declared() => new()
    {
        ModuleId = _moduleId,
        OrganizationId = _organizationId,
        ModuleName = "source",
        NamespaceName = "ns",
        StackName = "stack",
        RunnerId = _fixture.Runners["0"].Id,
        RunnerName = "runner",
        RunnerInstanceName = "instance-1",
        SourceRevision = "main",
        SourceUrl = "https://example.com/repo.git",
        SourceSubdirectory = "",
        Engine = "OpenTofu"
    };
}
