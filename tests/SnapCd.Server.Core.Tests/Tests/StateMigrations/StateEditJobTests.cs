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
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.ManualJobs;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.StateMigrations;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.StateMachine.ManualJobs.Activities;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;
using SnapCd.Server.Core.StateMachine.StateMigrations;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.StateMigrations;

/// <summary>
/// One state edit's job, from the setup steps through the pre-check, the approval gate, the edit
/// itself and the list that confirms it. The edit exiting cleanly is not proof an address arrived,
/// so the list that follows is what is reported.
///
/// Each edit closes this over its own saga and its own messages, so a reply that reached the wrong
/// job would fail here rather than being absorbed.
/// </summary>
public abstract class StateEditJobTests<
    TSaga, TStateMachine, TJobRequested, TApproved,
    TSelectRunnerInstanceRequested, TGetModuleRequested, TInitRequested,
    TSelectRunnerInstanceCompleted, TGetModuleCompleted, TInitCompleted,
    TPreCheckRequested, TPreCheckCompleted,
    TEditRequested, TEditCompleted, TEditFaulted> : IAsyncLifetime
    where TSaga : StateEditSagaBase, new()
    where TStateMachine : class, SagaStateMachine<TSaga>
    where TJobRequested : StateEditJobRequestedBase, new()
    where TApproved : ManualJobResumeEventBase, new()
    where TSelectRunnerInstanceRequested : ManualStepRequestBase, new()
    where TGetModuleRequested : ManualGetModuleRequestedBase, new()
    where TInitRequested : ManualStepRequestBase, new()
    where TSelectRunnerInstanceCompleted : ManualSelectRunnerInstanceCompletedBase, new()
    where TGetModuleCompleted : ManualGetModuleCompletedBase, new()
    where TInitCompleted : ManualStepResponseBase, new()
    where TPreCheckRequested : StateEditRequestBase, new()
    where TPreCheckCompleted : StateEditResponseBase, new()
    where TEditRequested : StateEditRequestBase, new()
    where TEditCompleted : StateEditResponseBase, new()
    where TEditFaulted : StepFaultedBase, new()
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private Guid _moduleId;
    private Guid _organizationId;
    private Guid _jobId;

    /// <summary>What this edit's job rows are filed as.</summary>
    protected abstract string JobType { get; }

    /// <summary>How this edit files an address it acted on.</summary>
    protected abstract AddressOperation RowOperation { get; }

    protected StateEditJobTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        _jobId = Guid.NewGuid();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddSingleton<IStateEncryptionService>(new StateEncryptionService(
            Options.Create(new StateStoreSettings { EncryptionKey = Convert.ToBase64String(new byte[32]) })));
        services.AddScoped<IMaintenanceModeService, MaintenanceModeService>();
        services.AddScoped<IPrincipalProvider>(_ =>
            new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_organizationId]));
        services.AddScoped<ManualModuleJobRepository>();
        services.AddScoped<ManualJobStepService>();
        services.AddScoped<ManualJobAddressService>();
        services.AddScoped(typeof(CancelManualModuleJobActivity<,>));
        services.AddScoped(typeof(PartiallyCompleteManualModuleJobActivity<,>));
        services.AddScoped(typeof(StateEditNeedsApprovalActivity<,>));
        services.AddScoped(typeof(SendStateEditStepToRunnerActivity<,,>));
        services.AddScoped(typeof(WaitingForApprovalManualJobActivity<,>));
        services.AddScoped(typeof(NotWaitingForApprovalManualJobActivity<,>));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<TStateMachine, TSaga>()
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
        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = _jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = JobType,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();

        await using var db = _fixture.CreateDbContext();
        await db.Set<TSaga>().Where(s => s.CorrelationId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobAddresses.Where(a => a.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobSteps.Where(s => s.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobApprovals.Where(a => a.ManualModuleJobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobs.Where(j => j.Id == _jobId).ExecuteDeleteAsync();
    }

    /// <summary>
    /// The edit reports what it managed; the list that follows asks the state about those
    /// addresses. A clean batch ends completed.
    /// </summary>
    [Fact]
    public async Task A_Whole_Batch_Completes()
    {
        await RunToEdit([("random_pet.old", "random_pet.new")]);

        await Answer(Completed([Succeeded("random_pet.old", "random_pet.new")]));

        Assert.True(await WaitUntil(() => InState("ListPending")), "the edit never asked the state");

        await Answer(new StateListFilteredCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Results = [new AddressResult { Address = "random_pet.old", Outcome = AddressOutcome.Absent }]
        });

        Assert.True(await WaitUntil(() => JobStatus() == ExecutionStatus.Completed), "the job did not complete");
    }

    /// <summary>
    /// A batch that manages some and not others is unfinished work with a remedy, not a failure.
    /// The addresses that failed are recorded so they can be run again.
    /// </summary>
    [Fact]
    public async Task A_Batch_That_Half_Worked_Ends_Partially_Completed()
    {
        await RunToEdit([("a.one", "a.two"), ("b.one", "b.two")]);

        await Answer(Completed([
            Succeeded("a.one", "a.two"),
            new AddressResult { Address = "b.one", Target = "b.two", Outcome = AddressOutcome.Failed }
        ]));

        Assert.True(await WaitUntil(() => InState("ListPending")), "the edit never asked the state");

        // Only the address the edit managed is asked about.
        var list = _harness.Published.Select<StateListFilteredRequested>()
            .Select(p => p.Context.Message).Last();

        Assert.Contains("a.one", list.Addresses);
        Assert.DoesNotContain("b.one", list.Addresses);

        await Answer(new StateListFilteredCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Results = [new AddressResult { Address = "a.one", Outcome = AddressOutcome.Absent }]
        });

        Assert.True(
            await WaitUntil(() => JobStatus() == ExecutionStatus.PartiallyCompleted),
            "the job was not recorded as partially completed");

        await using var db = _fixture.CreateDbContext();
        var rows = await db.ManualModuleJobAddresses.AsNoTracking()
            .Where(a => a.JobId == _jobId).ToListAsync();

        Assert.Equal(AddressOutcome.Failed,
            Assert.Single(rows.Where(r => r.Address == "b.one" && r.Operation == RowOperation)).Outcome);
    }

    /// <summary>
    /// The ledger closes from what the state says, not from what the command reported, so the two
    /// stay separate facts.
    /// </summary>
    [Fact]
    public async Task What_The_State_Says_Is_What_Is_Announced()
    {
        await RunToEdit([("random_pet.a", "random_pet.b")]);

        await Answer(Completed([Succeeded("random_pet.a", "random_pet.b")]));

        Assert.True(await WaitUntil(() => InState("ListPending")), "the edit never asked the state");

        await Answer(new StateListFilteredCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Results = [new AddressResult { Address = "random_pet.a", Outcome = AddressOutcome.Absent }]
        });

        Assert.True(await WaitUntil(() => JobStatus() != ExecutionStatus.Running), "the job never ended");

        await using var db = _fixture.CreateDbContext();
        var checkedRow = Assert.Single(await db.ManualModuleJobAddresses.AsNoTracking()
            .Where(a => a.JobId == _jobId && a.Operation == AddressOperation.List).ToListAsync());

        Assert.Equal(AddressOutcome.Absent, checkedRow.Outcome);
    }

    /// <summary>
    /// An edit that never ran writes nothing, so the job fails outright rather than being partly
    /// done.
    /// </summary>
    [Fact]
    public async Task A_Faulted_Edit_Fails_The_Job()
    {
        await RunToEdit([("random_pet.a", "random_pet.b")]);

        await Answer(new TEditFaulted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            ErrorMessage = "backend unreachable"
        });

        Assert.True(await WaitUntil(() => JobStatus() == ExecutionStatus.Failed), "the job did not fail");
    }

    /// <summary>
    /// The pre-check runs before anyone is asked to approve, so an approver has something concrete
    /// to answer against. Nothing is written until the threshold is met.
    /// </summary>
    [Fact]
    public async Task The_Edit_Waits_For_Approval()
    {
        await RunToPreCheck([("random_pet.a", "random_pet.b")]);

        await Answer(new TPreCheckCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Results = [Succeeded("random_pet.a", "random_pet.b")]
        });

        Assert.True(
            await WaitUntil(() => InState("WaitingForApproval")),
            "the edit did not wait to be approved");

        Assert.Empty(_harness.Published.Select<TEditRequested>());
    }

    /// <summary>
    /// Both ends of a move are filed separately and both are asked about afterwards. Only a move
    /// has two ends, so only a move runs this.
    /// </summary>
    protected async Task BothEndsAreRecorded()
    {
        await RunToEdit([("random_pet.old", "random_pet.new")]);

        await Answer(Completed([Succeeded("random_pet.old", "random_pet.new")]));

        Assert.True(await WaitUntil(() => InState("ListPending")), "the edit never asked the state");

        var list = _harness.Published.Select<StateListFilteredRequested>()
            .Select(p => p.Context.Message).Last();

        Assert.Contains("random_pet.old", list.Addresses);
        Assert.Contains("random_pet.new", list.Addresses);

        await using var db = _fixture.CreateDbContext();
        var rows = await db.ManualModuleJobAddresses.AsNoTracking()
            .Where(a => a.JobId == _jobId).OrderBy(a => a.Address).ToListAsync();

        Assert.Equal(2, rows.Count);

        Assert.Equal("random_pet.new", rows[0].Address);
        Assert.Equal("random_pet.old", rows[0].Target);
        Assert.Equal(AddressOperation.MoveTo, rows[0].Operation);

        Assert.Equal("random_pet.old", rows[1].Address);
        Assert.Equal("random_pet.new", rows[1].Target);
        Assert.Equal(AddressOperation.MoveFrom, rows[1].Operation);
    }

    private async Task Start(List<(string Address, string? Target)> instructions) =>
        await _harness.Bus.Publish(new TJobRequested
        {
            CorrelationId = _jobId,
            Declared = Declared(),
            Instructions = instructions
                .Select(i => new AddressInstruction { Address = i.Address, Target = i.Target })
                .ToList()
        });

    /// <summary>Drives the setup steps so a test can get straight to the pre-check.</summary>
    private async Task RunToPreCheck(List<(string Address, string? Target)> instructions)
    {
        await Start(instructions);

        var select = await AwaitStep<TSelectRunnerInstanceRequested>("SelectRunnerInstancePending");
        await Answer(new TSelectRunnerInstanceCompleted
        {
            CorrelationId = select.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            RunnerInstanceName = "runner-a"
        });

        var getModule = await AwaitStep<TGetModuleRequested>("GetModulePending");
        await Answer(new TGetModuleCompleted
        {
            CorrelationId = getModule.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId
        });

        var init = await AwaitStep<TInitRequested>("InitPending");
        await Answer(new TInitCompleted
        {
            CorrelationId = init.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId
        });

        await AwaitStep<TPreCheckRequested>("PreCheckPending");
    }

    /// <summary>
    /// Drives the setup steps, the pre-check and the approval, so a test can get straight to the
    /// edit. A state edit defaults to needing one approval, so it parks until one is recorded.
    /// </summary>
    private async Task RunToEdit(List<(string Address, string? Target)> instructions)
    {
        await RunToPreCheck(instructions);

        await Answer(new TPreCheckCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Results = instructions.Select(i => Succeeded(i.Address, i.Target)).ToList()
        });

        Assert.True(
            await WaitUntil(() => InState("WaitingForApproval")),
            "the edit did not wait to be approved");

        await Approve();

        Assert.True(await WaitUntil(() => InState("EditPending")), "the job never reached the edit");
    }

    /// <summary>Records the one approval a state edit needs, then tells the saga to look again.</summary>
    private async Task Approve()
    {
        await using var db = _fixture.CreateDbContext();
        db.ManualModuleJobApprovals.Add(new ManualModuleJobApproval
        {
            Id = Guid.NewGuid(),
            ManualModuleJobId = _jobId,
            OrganizationId = _organizationId,
            PrincipalId = Guid.NewGuid(),
            PrincipalDiscriminator = PrincipalDiscriminator.User,
            DecisionDateTime = DateTime.UtcNow,
            Declined = false
        });
        await db.SaveChangesAsync();

        await _harness.Bus.Publish(new ApprovalReevaluationRequestedEvent
        {
            ModuleJobId = _jobId,
            ModuleId = _moduleId
        });
    }

    private static AddressResult Succeeded(string address, string? target) => new()
    {
        Address = address, Target = target, Outcome = AddressOutcome.Succeeded
    };

    private TEditCompleted Completed(List<AddressResult> results) => new()
    {
        CorrelationId = _jobId,
        OrganizationId = _organizationId,
        Results = results
    };

    private ResolvedModule Declared() => new()
    {
        ModuleId = _moduleId,
        OrganizationId = _organizationId,
        ModuleName = "module",
        NamespaceName = "ns",
        StackName = "stack",
        RunnerId = _fixture.Runners["0"].Id,
        RunnerName = "runner",
        RunnerInstanceName = "runner-a",
        SourceRevision = "main",
        SourceUrl = "https://example.com/repo.git",
        SourceSubdirectory = "",
        Engine = "OpenTofu"
    };

    private async Task<T> AwaitStep<T>(string expectedState) where T : class
    {
        Assert.True(
            await WaitUntil(() => _harness.Published.Select<T>().Any()),
            $"{typeof(T).Name} was never asked for");

        Assert.True(
            await WaitUntil(() => InState(expectedState)),
            $"the job never reached {expectedState}");

        return _harness.Published.Select<T>().Select(p => p.Context.Message).Last();
    }

    private async Task Answer<T>(T message) where T : class =>
        await _harness.Bus.Publish(message);

    private bool InState(string state)
    {
        using var db = _fixture.CreateDbContext();
        return db.Set<TSaga>().AsNoTracking()
            .Any(x => x.CorrelationId == _jobId && x.CurrentState == state);
    }

    private ExecutionStatus JobStatus()
    {
        using var db = _fixture.CreateDbContext();
        return db.ManualModuleJobs.AsNoTracking()
            .Where(j => j.Id == _jobId).Select(j => j.Status).Single();
    }

    private static async Task<bool> WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100; i++)
        {
            if (condition()) return true;
            await Task.Delay(100);
        }

        return false;
    }
}

/// <summary>Moving addresses to where they should be, after a dry run says what would move.</summary>
[Collection("NewRoleBasedSharedFixture")]
public class MoveJobTests(Fixture fixture) : StateEditJobTests<
    MoveSaga, MoveStateMachine, MoveJobRequested, MoveApproved,
    MoveSelectRunnerInstanceRequested, MoveGetModuleRequested, MoveInitRequested,
    MoveSelectRunnerInstanceCompleted, MoveGetModuleCompleted, MoveInitCompleted,
    MoveDryRunRequested, MoveDryRunCompleted,
    MoveRequested, MoveCompleted, MoveFaulted>(fixture)
{
    protected override string JobType => ManualJobTypes.StateMove;

    protected override AddressOperation RowOperation => AddressOperation.MoveFrom;

    /// <summary>
    /// A move files both ends separately, so either address can be looked up by its own name, and
    /// both are asked about afterwards: a move that only half happened shows in neither end alone.
    /// </summary>
    [Fact]
    public async Task A_Move_Records_Both_Ends() => await BothEndsAreRecorded();
}

/// <summary>Bringing resources already there under management, after checking the addresses are free.</summary>
[Collection("NewRoleBasedSharedFixture")]
public class ImportJobTests(Fixture fixture) : StateEditJobTests<
    ImportSaga, ImportStateMachine, ImportJobRequested, ImportApproved,
    ImportSelectRunnerInstanceRequested, ImportGetModuleRequested, ImportInitRequested,
    ImportSelectRunnerInstanceCompleted, ImportGetModuleCompleted, ImportInitCompleted,
    ImportPreCheckRequested, ImportPreCheckCompleted,
    ImportRequested, ImportCompleted, ImportFaulted>(fixture)
{
    protected override string JobType => ManualJobTypes.StateImport;

    protected override AddressOperation RowOperation => AddressOperation.Import;
}

/// <summary>Taking addresses out of state, after a dry run says what would go.</summary>
[Collection("NewRoleBasedSharedFixture")]
public class RemoveJobTests(Fixture fixture) : StateEditJobTests<
    RemoveSaga, RemoveStateMachine, RemoveJobRequested, RemoveApproved,
    RemoveSelectRunnerInstanceRequested, RemoveGetModuleRequested, RemoveInitRequested,
    RemoveSelectRunnerInstanceCompleted, RemoveGetModuleCompleted, RemoveInitCompleted,
    RemoveDryRunRequested, RemoveDryRunCompleted,
    RemoveRequested, RemoveCompleted, RemoveFaulted>(fixture)
{
    protected override string JobType => ManualJobTypes.StateRemove;

    protected override AddressOperation RowOperation => AddressOperation.Remove;
}
