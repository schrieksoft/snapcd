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
/// Asking a Module's state about specific addresses. The job writes nothing, so it runs the
/// preamble and reports; what it found closes the transfer ledger without it knowing transfers
/// exist.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class StateListFilteredJobTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private Guid _moduleId;
    private Guid _organizationId;
    private Guid _jobId;

    public StateListFilteredJobTests(Fixture fixture) => _fixture = fixture;

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

        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<StateListFilteredStateMachine, StateListFilteredSaga>()
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
            JobType = ManualJobTypes.StateListFiltered,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();

        await using var db = _fixture.CreateDbContext();
        await db.Set<StateListFilteredSaga>().Where(s => s.CorrelationId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobAddresses.Where(a => a.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobSteps.Where(s => s.JobId == _jobId).ExecuteDeleteAsync();
        await db.ManualModuleJobs.Where(j => j.Id == _jobId).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Listing state needs a checkout and an initialised backend, so the job runs the same preamble
    /// as any other - and stops there, because it has nothing to plan.
    /// </summary>
    [Fact]
    public async Task It_Runs_The_Preamble_Then_Asks()
    {
        await RunToList(["random_pet.a"]);

        var list = Assert.Single(_harness.Published.Select<StateListFilteredRequested>()
            .Select(p => p.Context.Message));

        Assert.Equal(["random_pet.a"], list.Addresses);
    }

    /// <summary>The checkout the preamble fetched is recorded against the job, as on any other.</summary>
    [Fact]
    public async Task The_Revision_It_Ran_Against_Is_Recorded()
    {
        await RunToList(["random_pet.a"], definitiveRevision: "abc123");

        await using var db = _fixture.CreateDbContext();
        var saga = await db.Set<StateListFilteredSaga>().AsNoTracking()
            .SingleAsync(s => s.CorrelationId == _jobId);

        Assert.Equal("abc123", saga.DefinitiveRevision);
    }

    /// <summary>
    /// What was found is recorded per address and announced. Anything watching an address closes
    /// from this; the job itself knows nothing about what it might close.
    /// </summary>
    [Fact]
    public async Task What_It_Found_Is_Recorded_And_Announced()
    {
        await RunToList(["random_pet.a", "random_pet.b"]);

        await Answer(new StateListFilteredCompleted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            Results =
            [
                new AddressResult { Address = "random_pet.a", Outcome = AddressOutcome.Present },
                new AddressResult { Address = "random_pet.b", Outcome = AddressOutcome.Absent }
            ]
        });

        Assert.True(await WaitUntil(JobHasEnded), "the job row was never closed out");

        await using var db = _fixture.CreateDbContext();
        var rows = await db.ManualModuleJobAddresses.AsNoTracking()
            .Where(a => a.JobId == _jobId).OrderBy(a => a.Address).ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(AddressOperation.List, r.Operation));
        Assert.All(rows, r => Assert.Equal(AddressDirection.Checked, r.Direction));
        Assert.Equal(AddressOutcome.Present, rows[0].Outcome);
        Assert.Equal(AddressOutcome.Absent, rows[1].Outcome);

        var touched = Assert.Single(_harness.Published.Select<StateAddressesTouched>()
            .Select(p => p.Context.Message));

        Assert.Equal(["random_pet.a"], touched.Present);
        Assert.Equal(["random_pet.b"], touched.Absent);
    }

    /// <summary>A step that faults ends the job rather than leaving the row running forever.</summary>
    [Fact]
    public async Task A_Faulted_List_Ends_The_Job()
    {
        await RunToList(["random_pet.a"]);

        await Answer(new StateListFilteredFaulted
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId,
            ErrorMessage = "no state"
        });

        Assert.True(await WaitUntil(JobHasEnded), "the job row was never closed out");
        Assert.Empty(_harness.Published.Select<StateAddressesTouched>());
    }

    /// <summary>A job cancelled before the list runs reports nothing.</summary>
    [Fact]
    public async Task A_Cancelled_Job_Reports_Nothing()
    {
        await RunToList(["random_pet.a"]);

        await _harness.Bus.Publish(new CancelManualModuleJobRequested
        {
            CorrelationId = _jobId,
            OrganizationId = _organizationId
        });

        Assert.True(await WaitUntil(JobHasEnded), "the job row was never closed out");
        Assert.Empty(_harness.Published.Select<StateAddressesTouched>());
    }

    private async Task Start(List<string> addresses) =>
        await _harness.Bus.Publish(new StateListFilteredJobRequested
        {
            CorrelationId = _jobId,
            Declared = Declared(),
            Addresses = addresses
        });

    /// <summary>Drives the preamble so a test can get straight to the step it cares about.</summary>
    private async Task RunToList(List<string> addresses, string? definitiveRevision = null)
    {
        await Start(addresses);

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
            ModuleId = _moduleId,
            DefinitiveRevision = definitiveRevision
        });

        var init = await AwaitStep<TransferInitRequested>("InitPending");
        await Answer(new TransferInitCompleted
        {
            CorrelationId = init.CorrelationId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId
        });

        Assert.True(await WaitUntil(() => InState("ListPending")), "the job never reached the list");
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
        return db.Set<StateListFilteredSaga>().AsNoTracking()
            .Any(x => x.CorrelationId == _jobId && x.CurrentState == state);
    }

    private bool JobHasEnded()
    {
        using var db = _fixture.CreateDbContext();
        return db.ManualModuleJobs.AsNoTracking()
            .Any(j => j.Id == _jobId && j.Status != ExecutionStatus.Running);
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
