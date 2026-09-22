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
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.Transfers;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.StateMachine.Transfers;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Transfers;

/// <summary>
/// A prove round end to end, with the runner replaced by this test answering each step. It is the
/// first thing that shows the two Module sagas and their coordinator actually working together:
/// both Modules plan, the state fragment crosses from one to the other, and each proves in turn.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransferProveRoundTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Guid _sourceModuleId;
    private Guid _receiverModuleId;
    private Guid _organizationId;
    private Guid _transferId;
    private Guid _jobId;

    public TransferProveRoundTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _sourceModuleId = _fixture.Modules["0000"].Id;
        _receiverModuleId = _fixture.Modules["0001"].Id;
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
        services.AddScoped<ManualJobStepService>();
        services.AddScoped<TransferArtefactService>();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<TransferStateMachine, TransferSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
            x.AddSagaStateMachine<TransferParticipantStateMachine, TransferParticipantSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(20);
        await _harness.Start();

        await using var db = _fixture.CreateDbContext();
        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = _jobId,
            ModuleId = _sourceModuleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = ManualJobTypes.TransferProve,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();

        await using var db = _fixture.CreateDbContext();
        await db.Set<TransferSaga>().Where(s => s.TransferId == _transferId).ExecuteDeleteAsync();
        await db.Set<TransferParticipantSaga>().Where(s => s.TransferId == _transferId).ExecuteDeleteAsync();
        await db.ManualModuleJobArtefacts.Where(a => a.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobSteps.Where(s => s.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobs.Where(j => j.Id == _jobId).ExecuteDeleteAsync();
    }

    /// <summary>
    /// The whole round with neither Module needing a value from the other, so the two prove at
    /// once. Every runner step is answered here in the order the sagas ask for it.
    /// </summary>
    [Fact]
    public async Task A_Round_Where_Neither_Module_Needs_The_Other_Runs_Both_In_Turn()
    {
        await Open();
        await StartRound();

        // The source runs its whole sequence first: its state pull writes the fragment.
        await Preamble(_sourceModuleId);
        var sourceMap = await AwaitStep<TransferMigrateMapRequested>(_sourceModuleId, "MigrateMapPending");
        Assert.Null(sourceMap.FragmentState);
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = sourceMap.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _sourceModuleId,
            FragmentState = "{\"serial\":4}",
            FragmentMeta = "map_hash: abc",
            NeedsValuesFrom = []
        });

        var sourceProve = await AwaitStep<TransferMigrateProveRequested>(_sourceModuleId, "MigrateProvePending");
        await Answer(new TransferMigrateProveCompleted
        {
            CorrelationId = sourceProve.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _sourceModuleId,
            ExitCode = 0
        });

        // Only then does the receiver start, and it is given the fragment.
        await Preamble(_receiverModuleId);
        var receiverMap = await AwaitStep<TransferMigrateMapRequested>(_receiverModuleId, "MigrateMapPending");
        Assert.Equal("{\"serial\":4}", receiverMap.FragmentState);
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = receiverMap.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _receiverModuleId
        });

        var receiverProve = await AwaitStep<TransferMigrateProveRequested>(_receiverModuleId, "MigrateProvePending");
        await Answer(new TransferMigrateProveCompleted
        {
            CorrelationId = receiverProve.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _receiverModuleId,
            ExitCode = 0
        });

        Assert.True(await WaitForCoordinator(s => s.CurrentState == "Idle"),
            "the round did not finish");

        // Nothing a round carried outlives it.
        await using var db = _fixture.CreateDbContext();
        Assert.Empty(await db.ManualModuleJobArtefacts.Where(a => a.JobId == _jobId).ToListAsync());
    }

    /// <summary>
    /// A refused proof stops the round where it is, and both Modules are told to stop rather than
    /// one being left running.
    /// </summary>
    [Fact]
    public async Task A_Refused_Proof_Stops_The_Round()
    {
        await Open();
        await StartRound();

        await Preamble(_sourceModuleId);
        var sourceMap = await AwaitStep<TransferMigrateMapRequested>(_sourceModuleId, "MigrateMapPending");
        await Answer(new TransferMigrateMapCompleted
        {
            CorrelationId = sourceMap.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _sourceModuleId,
            FragmentState = "{}",
            NeedsValuesFrom = []
        });

        var sourceProve = await AwaitStep<TransferMigrateProveRequested>(_sourceModuleId, "MigrateProvePending");
        await Answer(new TransferMigrateProveCompleted
        {
            CorrelationId = sourceProve.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _sourceModuleId,
            ExitCode = 2,
            Verdict = "one resource would be created"
        });

        Assert.True(await WaitForCoordinator(s => s.CurrentState == "Stalled"),
            "a refused proof did not stop the round");

        var saga = await Coordinator();
        Assert.Contains("did not plan clean", saga!.StallReason);
    }

    /// <summary>A dirty plan is not a transfer: the round stops before any state is pulled.</summary>
    [Fact]
    public async Task A_Dirty_Plan_Stops_The_Round_Before_Any_State_Is_Pulled()
    {
        await Open();
        await StartRound();

        await Preamble(_sourceModuleId, totalChangedCount: 3);

        Assert.True(await WaitForCoordinator(s => s.CurrentState == "Stalled"),
            "a dirty plan did not stop the round");

        Assert.Empty(_harness.Published.Select<TransferMigrateMapRequested>());
    }

    private async Task Open()
    {
        await _harness.Bus.Publish(new TransferOpened
        {
            CorrelationId = Guid.NewGuid(),
            OrganizationId = _organizationId,
            TransferId = _transferId,
            SourceModuleId = _sourceModuleId,
            ReceiverModuleId = _receiverModuleId,
            MapHash = "abc",
            SourceDeclared = Declared(_sourceModuleId, "source"),
            ReceiverDeclared = Declared(_receiverModuleId, "receiver")
        });

        Assert.True(await WaitForCoordinator(s => s.CurrentState == "Idle"),
            "the transfer did not open");
    }

    private async Task StartRound()
    {
        var saga = await Coordinator();

        await _harness.Bus.Publish(new TransferProveRoundRequested
        {
            CorrelationId = saga!.CorrelationId,
            OrganizationId = _organizationId,
            JobId = _jobId,
            Map = "version: 1",
            SourceProveRef = "main",
            ReceiverProveRef = "main"
        });
    }

    /// <summary>Answers one Module's four preamble steps in the order it asks for them.</summary>
    private async Task Preamble(Guid moduleId, int totalChangedCount = 0)
    {
        var select = await AwaitStep<TransferSelectRunnerInstanceRequested>(moduleId, "SelectRunnerInstancePending");
        await Answer(new TransferSelectRunnerInstanceCompleted
        {
            CorrelationId = select.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            RunnerInstanceName = "instance-1"
        });

        var get = await AwaitStep<TransferGetModuleRequested>(moduleId, "GetModulePending");
        Assert.Equal("main", get.SourceRevisionOverride);
        await Answer(new TransferGetModuleCompleted
        {
            CorrelationId = get.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            DefinitiveRevision = "abc123"
        });

        var init = await AwaitStep<TransferInitRequested>(moduleId, "InitPending");
        await Answer(new TransferInitCompleted
        {
            CorrelationId = init.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = moduleId
        });

        var validate = await AwaitStep<TransferValidateRequested>(moduleId, "ValidatePending");
        await Answer(new TransferValidateCompleted
        {
            CorrelationId = validate.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = moduleId
        });

        var plan = await AwaitStep<TransferPlanRequested>(moduleId, "PlanPending");
        await Answer(new TransferPlanCompleted
        {
            CorrelationId = plan.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            TotalChangedCount = totalChangedCount
        });
    }

    /// <summary>
    /// The request a Module is waiting on, which is what a runner would pick up. It also waits for
    /// that Module's saga to settle in the state that accepts the reply: a request is published
    /// from inside the handler, before the transition it belongs to has committed.
    /// </summary>
    private async Task<T> AwaitStep<T>(Guid moduleId, string expectedState) where T : TransferStepRequestBase
    {
        Assert.True(
            await WaitUntil(() => _harness.Published.Select<T>().Any(m => m.Context.Message.ModuleId == moduleId)),
            $"{typeof(T).Name} was never asked of Module {moduleId}");

        Assert.True(
            await WaitUntil(() =>
            {
                using var db = _fixture.CreateDbContext();
                return db.Set<TransferParticipantSaga>().AsNoTracking()
                    .Any(x => x.TransferId == _transferId && x.ModuleId == moduleId && x.CurrentState == expectedState);
            }),
            $"Module {moduleId} never settled in {expectedState}");

        return _harness.Published.Select<T>().First(m => m.Context.Message.ModuleId == moduleId).Context.Message;
    }

    private Task Answer<T>(T message) where T : class => _harness.Bus.Publish(message);

    private async Task<TransferSaga?> Coordinator()
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Set<TransferSaga>().AsNoTracking()
            .SingleOrDefaultAsync(s => s.TransferId == _transferId);
    }

    private Task<bool> WaitForCoordinator(Func<TransferSaga, bool> predicate) =>
        WaitUntil(() =>
        {
            using var db = _fixture.CreateDbContext();
            var saga = db.Set<TransferSaga>().AsNoTracking().SingleOrDefault(s => s.TransferId == _transferId);
            return saga != null && predicate(saga);
        });

    private static async Task<bool> WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            await Task.Delay(100);
        }

        return condition();
    }

    private ResolvedModule Declared(Guid moduleId, string name) => new()
    {
        ModuleId = moduleId,
        OrganizationId = _organizationId,
        ModuleName = name,
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
