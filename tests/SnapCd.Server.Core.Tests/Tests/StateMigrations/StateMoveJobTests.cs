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
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Crud.StateMigrations;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;
using SnapCd.Server.Core.StateMachine.StateMigrations;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.StateMigrations;

/// <summary>
/// Moving addresses in a Module's state, then asking the state what is there. The move exiting
/// cleanly is not proof the address arrived, so the list that follows is what is reported.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class StateMoveJobTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private Guid _moduleId;
    private Guid _organizationId;
    private Guid _jobId;

    public StateMoveJobTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
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
        services.AddScoped<ManualJobAddressService>();
        services.AddScoped(typeof(CancelManualModuleJobActivity<,>));
        services.AddScoped(typeof(PartiallyCompleteManualModuleJobActivity<,>));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<StateMoveStateMachine, StateMoveSaga>()
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
            JobType = ManualJobTypes.StateMv,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();

        await using var db = _fixture.CreateDbContext();
        await db.Set<StateMoveSaga>().Where(s => s.CorrelationId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobAddresses.Where(a => a.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobSteps.Where(s => s.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobs.Where(j => j.Id == _jobId).ExecuteDeleteAsync();
    }

    /// <summary>
    /// The move reports what it managed; the list that follows asks the state about those
    /// addresses. A clean batch ends completed.
    /// </summary>
    [Fact]
    public async Task A_Whole_Batch_Completes()
    {
        await RunToMove([("random_pet.old", "random_pet.new")]);

        await Answer(new StateMoveCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Operation = AddressOperation.Mv,
            Results = [new AddressResult
            {
                Address = "random_pet.old", Target = "random_pet.new", Outcome = AddressOutcome.Succeeded
            }]
        });

        Assert.True(await WaitUntil(() => InState("ListPending")), "the move never asked the state");

        var list = _harness.Published.Select<StateListFilteredRequested>()
            .Select(p => p.Context.Message).Last();

        Assert.Equal(["random_pet.old"], list.Addresses);

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
        await RunToMove([("a.one", "a.two"), ("b.one", "b.two")]);

        await Answer(new StateMoveCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Operation = AddressOperation.Mv,
            Results =
            [
                new AddressResult { Address = "a.one", Target = "a.two", Outcome = AddressOutcome.Succeeded },
                new AddressResult { Address = "b.one", Target = "b.two", Outcome = AddressOutcome.Failed }
            ]
        });

        Assert.True(await WaitUntil(() => InState("ListPending")), "the move never asked the state");

        // Only the address the move managed is asked about.
        var list = _harness.Published.Select<StateListFilteredRequested>()
            .Select(p => p.Context.Message).Last();

        Assert.Equal(["a.one"], list.Addresses);

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
            Assert.Single(rows.Where(r => r.Address == "b.one" && r.Direction == AddressDirection.Left)).Outcome);
    }

    /// <summary>An mv is two halves on one Module: the address it leaves and the one it arrives at.</summary>
    [Fact]
    public async Task An_Mv_Records_Both_Halves()
    {
        await RunToMove([("random_pet.old", "random_pet.new")]);

        await Answer(new StateMoveCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Operation = AddressOperation.Mv,
            Results = [new AddressResult
            {
                Address = "random_pet.old", Target = "random_pet.new", Outcome = AddressOutcome.Succeeded
            }]
        });

        Assert.True(await WaitUntil(() => InState("ListPending")), "the move never asked the state");

        await using var db = _fixture.CreateDbContext();
        var rows = await db.ManualModuleJobAddresses.AsNoTracking()
            .Where(a => a.JobId == _jobId).OrderBy(a => a.Address).ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal("random_pet.new", rows[0].Address);
        Assert.Equal(AddressDirection.Arrived, rows[0].Direction);
        Assert.Equal("random_pet.old", rows[1].Address);
        Assert.Equal(AddressDirection.Left, rows[1].Direction);
    }

    /// <summary>
    /// The ledger closes from what the state says, not from what the command reported, so the two
    /// stay separate facts.
    /// </summary>
    [Fact]
    public async Task What_The_State_Says_Is_What_Is_Announced()
    {
        await RunToMove([("random_pet.a", null)], AddressOperation.Remove);

        await Answer(new StateMoveCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Operation = AddressOperation.Remove,
            Results = [new AddressResult { Address = "random_pet.a", Outcome = AddressOutcome.Succeeded }]
        });

        Assert.True(await WaitUntil(() => InState("ListPending")), "the move never asked the state");

        await Answer(new StateListFilteredCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Results = [new AddressResult { Address = "random_pet.a", Outcome = AddressOutcome.Absent }]
        });

        Assert.True(await WaitUntil(() => JobStatus() != ExecutionStatus.Running), "the job never ended");

        var touched = Assert.Single(_harness.Published.Select<StateAddressesTouched>()
            .Select(p => p.Context.Message));

        Assert.Equal(["random_pet.a"], touched.Absent);
        Assert.Empty(touched.Present);
    }

    /// <summary>
    /// A move that never ran writes nothing, so the job fails outright rather than being partly
    /// done.
    /// </summary>
    [Fact]
    public async Task A_Faulted_Move_Fails_The_Job()
    {
        await RunToMove([("random_pet.a", null)], AddressOperation.Remove);

        await Answer(new StateMoveFaulted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            ErrorMessage = "backend unreachable"
        });

        Assert.True(await WaitUntil(() => JobStatus() == ExecutionStatus.Failed), "the job did not fail");
        Assert.Empty(_harness.Published.Select<StateAddressesTouched>());
    }

    private async Task Start(List<(string Address, string? Target)> instructions, AddressOperation operation) =>
        await _harness.Bus.Publish(new StateMoveJobRequested
        {
            CorrelationId = _jobId,
            Declared = Declared(),
            Operation = operation,
            Instructions = instructions
                .Select(i => new AddressInstruction { Address = i.Address, Target = i.Target })
                .ToList()
        });

    /// <summary>Drives the preamble so a test can get straight to the step it cares about.</summary>
    private async Task RunToMove(
        List<(string Address, string? Target)> instructions,
        AddressOperation operation = AddressOperation.Mv)
    {
        await Start(instructions, operation);

        var select = await AwaitStep<TransferSelectRunnerInstanceRequested>("SelectRunnerInstancePending");
        await Answer(new TransferSelectRunnerInstanceCompleted
        {
            CorrelationId = select.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            RunnerInstanceName = "runner-a"
        });

        var getModule = await AwaitStep<TransferGetModuleRequested>("GetModulePending");
        await Answer(new TransferGetModuleCompleted
        {
            CorrelationId = getModule.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId
        });

        var init = await AwaitStep<TransferInitRequested>("InitPending");
        await Answer(new TransferInitCompleted
        {
            CorrelationId = init.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId
        });

        Assert.True(await WaitUntil(() => InState("MovePending")), "the job never reached the move");
    }

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
        return db.Set<StateMoveSaga>().AsNoTracking()
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
