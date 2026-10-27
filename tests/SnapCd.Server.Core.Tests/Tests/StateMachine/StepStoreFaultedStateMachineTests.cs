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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.Dto.Variables;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Server.Core.Consumers.Tasks.Handlers;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Variables;
using SnapCd.Server.Core.Services.Crud;
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
using DestroyMachine = SnapCd.Server.Core.StateMachine.Jobs.JobStateMachine<
    SnapCd.Server.Core.Entities.Sagas.DestroyJobSaga,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleRequested,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleFailed,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleCompleted,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyGetDefinitiveRevisionRequested,
    SnapCd.Server.Core.Events.Steps.DestroyGetDefinitiveRevisionCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyGetDefinitiveRevisionCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyGetDefinitiveRevisionFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyPolicyValidateRequested,
    SnapCd.Server.Core.Events.Steps.DestroyPolicyValidateCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyPolicyValidateCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyPolicyValidateFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyOutputRequested,
    SnapCd.Server.Core.Events.Steps.DestroyOutputCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyOutputCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyOutputFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyGetModuleRequested,
    SnapCd.Server.Core.Events.Steps.DestroyGetModuleCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyGetModuleCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyGetModuleFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyInitRequested,
    SnapCd.Server.Core.Events.Steps.DestroyInitCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyInitCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyInitFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyValidateRequested,
    SnapCd.Server.Core.Events.Steps.DestroyValidateCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyValidateCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyValidateFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyVariablesRequested,
    SnapCd.Server.Core.Events.Steps.DestroyVariablesCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyVariablesCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyVariablesFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyPlanRequested,
    SnapCd.Server.Core.Events.Steps.DestroyPlanCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyPlanCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyPlanFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanRequested,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanCancelled,
    SnapCd.Server.Core.Events.Steps.DummyDestroyProbeRequested,
    SnapCd.Server.Core.Events.Steps.DummyDestroyProbeCompleted,
    SnapCd.Server.Core.Events.Steps.DummyDestroyProbeCancelled,
    SnapCd.Server.Core.Events.Steps.DummyDestroyProbeFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyCancelKillRequested,
    SnapCd.Server.Core.Events.Steps.DummyDestroyCancelKillCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyCancelKillCompleted>;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// A failure while the server stores a step's variable or output set fails the job as a server-side error
/// on that step, for both job families. Sagas are seeded at the pending state, then the invoked event that
/// the runner's completion produces is published to the real consumer.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class StepStoreFaultedStateMachineTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Module _module = null!;
    private Runner _runner = null!;

    public StepStoreFaultedStateMachineTests(Fixture fixture)
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
        services.AddScoped(sp =>
        {
            var principalProvider = sp.GetRequiredService<IPrincipalProvider>();
            return new VariableSetService(new VariableSetSecuredRepository(
                new VariableSetRepository(sp.GetRequiredService<SnapCdDbContext>(), principalProvider, _fixture.CreateMockBus(),
                    Options.Create(new VariableSetRepositorySettings()), _fixture.CreateMockQuotaService()),
                principalProvider));
        });

        // The output store is not reached: the job lookup before it fails, which is the server-side error under test.
        services.AddScoped(sp => new ApplyOutputCompletedInvokedConsumer(
            sp.GetRequiredService<ILogger<ApplyOutputCompletedInvokedConsumer>>(), new UnavailableDbContextFactory(), null!, sp.GetRequiredService<IBus>()));
        services.AddScoped(sp => new DestroyOutputCompletedInvokedConsumer(
            sp.GetRequiredService<ILogger<DestroyOutputCompletedInvokedConsumer>>(), new UnavailableDbContextFactory(), null!, sp.GetRequiredService<IBus>()));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<ApplyVariablesCompletedInvokedConsumer>();
            x.AddConsumer<DestroyVariablesCompletedInvokedConsumer>();
            x.AddConsumer<ApplyOutputCompletedInvokedConsumer>();
            x.AddConsumer<DestroyOutputCompletedInvokedConsumer>();
            x.AddSagaStateMachine<ApplyMachine, ApplyJobSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
            x.AddSagaStateMachine<DestroyMachine, DestroyJobSaga>()
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

    private string DeclaredJson()
    {
        return JsonSerializer.Serialize(new ResolvedModule
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
        });
    }

    private async Task<Guid> SeedJob(bool destroy, string state)
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
                DeclaredJson = DeclaredJson()
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
                DeclaredJson = DeclaredJson()
            });

        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task<ModuleJob> WaitForFailedJob(Guid jobId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            await using var db = _fixture.CreateDbContext();
            var job = await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId && j.OrganizationId == _module.OrganizationId);
            if (job.Status == ExecutionStatus.Failed || DateTime.UtcNow >= deadline) return job;
            await Task.Delay(100);
        }
    }

    private static VariableSetCreateDto DuplicateNameVariableSet(Guid moduleId)
    {
        return new VariableSetCreateDto
        {
            ModuleId = moduleId,
            Checksum = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Variables =
            [
                new VariableCreateDto { Name = "extra_tenant_id" },
                new VariableCreateDto { Name = "extra_tenant_id", FromExtraFile = true }
            ]
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Variable_Set_Store_Failure_Fails_Job_As_Server_Side_Error(bool destroy)
    {
        var jobId = await SeedJob(destroy, "VariablesPending");

        if (destroy)
            await _harness.Bus.Publish(new DestroyVariablesCompletedInvoked { JobId = jobId, VariableSet = DuplicateNameVariableSet(_module.Id) });
        else
            await _harness.Bus.Publish(new ApplyVariablesCompletedInvoked { JobId = jobId, VariableSet = DuplicateNameVariableSet(_module.Id) });

        var job = await WaitForFailedJob(jobId);

        Assert.Equal(ExecutionStatus.Failed, job.Status);
        Assert.Equal(ServerSideStep.Variables, job.FailedOnServerSideStep);
        Assert.Equal("Server error during Variables", job.ServerSideErrorHeader);
        Assert.Contains("extra_tenant_id", job.ServerSideError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Output_Set_Store_Failure_Fails_Job_As_Server_Side_Error(bool destroy)
    {
        var jobId = await SeedJob(destroy, "OutputPending");
        var outputSet = new OutputSetCreateDto
        {
            Checksum = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        if (destroy)
            await _harness.Bus.Publish(new DestroyOutputCompletedInvoked { JobId = jobId, OutputSet = outputSet });
        else
            await _harness.Bus.Publish(new ApplyOutputCompletedInvoked { JobId = jobId, OutputSet = outputSet });

        var job = await WaitForFailedJob(jobId);

        Assert.Equal(ExecutionStatus.Failed, job.Status);
        Assert.Equal(ServerSideStep.Output, job.FailedOnServerSideStep);
        Assert.Equal("Server error during Output", job.ServerSideErrorHeader);
        Assert.Contains(UnavailableDbContextFactory.Message, job.ServerSideError);
    }

    private sealed class UnavailableDbContextFactory : IDbContextFactory<SnapCdDbContext>
    {
        public const string Message = "database unavailable";

        public SnapCdDbContext CreateDbContext() => throw new InvalidOperationException(Message);
    }
}
