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
using Microsoft.Extensions.Logging;
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
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
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
    private Guid _counterpartyId;
    private Guid _jobId;

    public TransferMigrateJobTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _counterpartyId = _fixture.Modules["0001"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
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
        services.AddScoped(typeof(TransferOutputsAvailableActivity<>));
        services.AddScoped(typeof(WaitingForApprovalManualJobActivity<,>));
        services.AddScoped(typeof(NotWaitingForApprovalManualJobActivity<,>));
        services.AddScoped(typeof(CancelManualModuleJobActivity<,>));
        services.AddScoped(typeof(RunnerConnectedActivity<,>));
        services.AddScoped(typeof(CheckRunnerConnectionActivity<,>));
        services.AddScoped(typeof(NotWaitingForRunnerActivity<,>));
        services.AddScoped(typeof(WaitingForRunnerActivity<,>));
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

        // Dispatch parks when the pinned runner has no connection, so the harness has to have one:
        // without it every step waits instead of being sent.
        db.RunnerConnections.Add(new RunnerConnection
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            RunnerId = _fixture.Runners["0"].Id,
            InstanceName = "instance-1",
            SignalRConnectionId = Guid.NewGuid().ToString("N"),
            ServerInstanceId = Guid.NewGuid()
        });

        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = _jobId,
            ModuleId = _moduleId,
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
        await db.Set<TransferMigrateSaga>().Where(s => s.CorrelationId == _jobId).ExecuteDeleteAsync();
        (await db.Modules.SingleAsync(m => m.Id == _moduleId)).StateMigrationApprovalThreshold = null;
        await db.SaveChangesAsync();

        await db.ManualModuleJobApprovals.Where(a => a.ManualModuleJobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobSteps.Where(s => s.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobs.Where(j => j.Id == _jobId).ExecuteDeleteAsync();
        await db.RunnerConnections.Where(rc => rc.OrganizationId == _organizationId).ExecuteDeleteAsync();
        await db.OutputSets.Where(o => o.ModuleId == _counterpartyId).ExecuteDeleteAsync();
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
            ModuleId = _moduleId
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

    /// <summary>
    /// A Module whose plan reads nothing from the other side proves straight away. This is the
    /// usual case, and it is what lets both Modules run at once.
    /// </summary>
    [Fact]
    public async Task A_Module_Needing_No_Outputs_Proves_Immediately()
    {
        await Start();
        await Preamble();

        var map = await AwaitStep<TransferMigrateMapRequested>("MigrateMapPending");
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = map.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            NeedsOutputs = []
        });

        Assert.NotNull(await AwaitStep<TransferMigrateProveRequested>("MigrateProvePending"));
    }

    /// <summary>
    /// A Module whose plan reads a value the other one produces cannot prove until that value
    /// exists, so it parks instead of planning against a value that is not there.
    /// </summary>
    [Fact]
    public async Task A_Module_Needing_An_Absent_Output_Waits()
    {
        await Start();
        await Preamble();

        var map = await AwaitStep<TransferMigrateMapRequested>("MigrateMapPending");
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = map.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            NeedsOutputs = ["db_endpoint"]
        });

        Assert.True(await WaitUntil(() => InState("WaitingForOutputs")), "the job did not wait for outputs");
        Assert.Empty(_harness.Published.Select<TransferMigrateProveRequested>());
    }

    /// <summary>A reevaluation with the value still missing leaves the job where it is.</summary>
    [Fact]
    public async Task A_Reevaluation_Without_The_Output_Keeps_Waiting()
    {
        await Start();
        await Preamble();

        var map = await AwaitStep<TransferMigrateMapRequested>("MigrateMapPending");
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = map.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            NeedsOutputs = ["db_endpoint"]
        });

        Assert.True(await WaitUntil(() => InState("WaitingForOutputs")), "the job did not wait for outputs");

        await _harness.Bus.Publish(new OutputsReevaluationRequestedEvent
        {
            ModuleJobId = _jobId,
            OrganizationId = _organizationId
        });

        Assert.True(await WaitUntil(() => InState("WaitingForOutputs")), "the job left the wait");
        Assert.Empty(_harness.Published.Select<TransferMigrateProveRequested>());
    }

    /// <summary>
    /// Once the other Module has published the value, a reevaluation lets the job through. This is
    /// the path the wake consumer drives when an output set arrives.
    /// </summary>
    [Fact]
    public async Task The_Output_Arriving_Releases_The_Job()
    {
        await Start();
        await Preamble();

        var map = await AwaitStep<TransferMigrateMapRequested>("MigrateMapPending");
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = map.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            NeedsOutputs = ["db_endpoint"]
        });

        Assert.True(await WaitUntil(() => InState("WaitingForOutputs")), "the job did not wait for outputs");

        await SeedCounterpartyOutput("db_endpoint");

        await _harness.Bus.Publish(new OutputsReevaluationRequestedEvent
        {
            ModuleJobId = _jobId,
            OrganizationId = _organizationId
        });

        Assert.NotNull(await AwaitStep<TransferMigrateProveRequested>("MigrateProvePending"));
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
            ModuleId = _moduleId
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
            ModuleId = _moduleId
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

    private async Task Start(bool awaitConsent = false, Guid? transferId = null) =>
        await _harness.Bus.Publish(new TransferMigrateRequested
        {
            CorrelationId = _jobId,
            CounterpartyModuleId = _counterpartyId,
            TransferId = transferId,
            AwaitConsent = awaitConsent,
            Declared = Declared(),
            ProveRef = "main"
        });

    /// <summary>
    /// The Module that starts a transfer waits for the other to agree before anything runs, and
    /// the wait is a step on the job rather than a state nothing shows.
    /// </summary>
    [Fact]
    public async Task The_Starting_Module_Waits_To_Be_Agreed_To()
    {
        var transferId = Guid.NewGuid();
        await Start(awaitConsent: true, transferId: transferId);

        Assert.True(await WaitUntil(() => InState("WaitingForConsent")), "the job did not wait");
        Assert.Empty(_harness.Published.Select<TransferSelectRunnerInstanceRequested>());
    }

    /// <summary>Agreeing releases it, and only then does anything reach a runner.</summary>
    [Fact]
    public async Task Agreeing_Lets_The_Waiting_Module_Go_Ahead()
    {
        var transferId = Guid.NewGuid();
        await Start(awaitConsent: true, transferId: transferId);

        Assert.True(await WaitUntil(() => InState("WaitingForConsent")), "the job did not wait");

        await _harness.Bus.Publish(new ConsentDecided
        {
            TransferId = transferId,
            ModuleId = _counterpartyId,
            OrganizationId = _organizationId,
            Granted = true
        });

        Assert.NotNull(await AwaitStep<TransferSelectRunnerInstanceRequested>("SelectRunnerInstancePending"));
    }

    /// <summary>A refusal ends the waiting job: there is nothing for it to move into.</summary>
    [Fact]
    public async Task A_Refusal_Ends_The_Waiting_Module()
    {
        var transferId = Guid.NewGuid();
        await Start(awaitConsent: true, transferId: transferId);

        Assert.True(await WaitUntil(() => InState("WaitingForConsent")), "the job did not wait");

        await _harness.Bus.Publish(new ConsentDecided
        {
            TransferId = transferId,
            ModuleId = _counterpartyId,
            OrganizationId = _organizationId,
            Granted = false
        });

        Assert.True(await WaitUntil(JobHasEnded), "the job row was never closed out");
        Assert.Empty(_harness.Published.Select<TransferSelectRunnerInstanceRequested>());
    }

    /// <summary>The counterparty never waits: it is answering, not asking.</summary>
    [Fact]
    public async Task A_Module_That_Is_Not_Waiting_Starts_Straight_Away()
    {
        await Start();

        Assert.NotNull(await AwaitStep<TransferSelectRunnerInstanceRequested>("SelectRunnerInstancePending"));
    }

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

    /// <summary>An output set on the other Module of this transfer, which is what the gate reads.</summary>
    private async Task SeedCounterpartyOutput(string name)
    {
        await using var db = _fixture.CreateDbContext();
        db.OutputSets.Add(new OutputSet
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = _counterpartyId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Checksum = Guid.NewGuid().ToString("N"),
            Outputs =
            [
                new Output
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Name = name,
                    Type = "string"
                }
            ]
        });
        await db.SaveChangesAsync();
    }
}
