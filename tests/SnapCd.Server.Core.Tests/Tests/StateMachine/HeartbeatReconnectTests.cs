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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Consumers.Tasks;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
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
/// A runner that reconnects keeps its job while the reconnect is within the heartbeat threshold, and a job the
/// server gives up on carries the reason as a server-side error on the step it was in.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class HeartbeatReconnectTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Module _module = null!;
    private Runner _runner = null!;

    public HeartbeatReconnectTests(Fixture fixture)
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
    }

    private async Task<string> SeedConnection(TimeSpan age)
    {
        var instanceName = $"reconnect-{Guid.NewGuid():N}"[..30];
        var connectionId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.RunnerConnections.Add(new RunnerConnection
        {
            Id = connectionId,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            InstanceName = instanceName,
            SignalRConnectionId = $"conn-{connectionId:N}",
            ServerInstanceId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        // CreatedDateTime is stamped on save, so age it afterwards.
        await db.RunnerConnections
            .Where(rc => rc.Id == connectionId)
            .ExecuteUpdateAsync(u => u.SetProperty(rc => rc.CreatedDateTime, DateTime.UtcNow - age));

        return instanceName;
    }

    private async Task<object> CheckHeartbeat(string instanceName)
    {
        object? response = null;

        var contextMock = new Mock<ConsumeContext<ModuleJobHeartbeatRequested>>();
        contextMock.SetupGet(c => c.Message).Returns(new ModuleJobHeartbeatRequested
        {
            CorrelationId = Guid.NewGuid(),
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            RunnerInstanceName = instanceName
        });
        contextMock.Setup(c => c.RespondAsync(It.IsAny<HeartbeatCompleted>()))
            .Callback<HeartbeatCompleted>(m => response = m)
            .Returns(Task.CompletedTask);
        contextMock.Setup(c => c.RespondAsync(It.IsAny<HeartbeatFailed>()))
            .Callback<HeartbeatFailed>(m => response = m)
            .Returns(Task.CompletedTask);

        var consumer = new ModuleJobHeartbeatConsumer(NullLogger<ModuleJobHeartbeatConsumer>.Instance,
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        await consumer.Consume(contextMock.Object);

        Assert.NotNull(response);
        return response;
    }

    [Fact]
    public async Task Missing_Job_Record_After_A_Recent_Reconnect_Is_Healthy()
    {
        var instanceName = await SeedConnection(TimeSpan.FromSeconds(5));

        var response = await CheckHeartbeat(instanceName);

        Assert.IsType<HeartbeatCompleted>(response);
    }

    [Fact]
    public async Task Missing_Job_Record_Long_After_A_Reconnect_Fails_With_Reason()
    {
        var instanceName = await SeedConnection(TimeSpan.FromMinutes(5));

        var response = await CheckHeartbeat(instanceName);

        var failed = Assert.IsType<HeartbeatFailed>(response);
        Assert.Contains("has not reported on this job", failed.Reason);
    }

    [Fact]
    public async Task Missing_Job_Record_With_No_Connection_Fails_With_Reason()
    {
        var response = await CheckHeartbeat($"absent-{Guid.NewGuid():N}");

        var failed = Assert.IsType<HeartbeatFailed>(response);
        Assert.Contains("is not connected", failed.Reason);
    }

    [Fact]
    public async Task Failed_Heartbeat_Records_Server_Side_Error_On_The_Running_Step()
    {
        var jobId = Guid.NewGuid();
        var heartbeatRequestId = Guid.NewGuid();

        await using (var db = _fixture.CreateDbContext())
        {
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
                CurrentState = "ApplyFromPlanPending",
                ModuleId = _module.Id,
                OrganizationId = _module.OrganizationId,
                RunnerId = _runner.Id,
                RunnerName = _runner.Name,
                RunnerInstanceName = "harness",
                HeartbeatRequestId = heartbeatRequestId,
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
        }

        await _harness.Bus.Publish(
            new HeartbeatFailed { CorrelationId = jobId, OrganizationId = _module.OrganizationId, Reason = "Runner instance 'harness' is not connected." },
            c => c.RequestId = heartbeatRequestId);

        ModuleJob job;
        var deadline = DateTime.UtcNow.AddSeconds(10);
        do
        {
            await Task.Delay(100);
            await using var db = _fixture.CreateDbContext();
            job = await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId && j.OrganizationId == _module.OrganizationId);
        } while (job.Status != ExecutionStatus.Failed && DateTime.UtcNow < deadline);

        Assert.Equal(ExecutionStatus.Failed, job.Status);
        Assert.Equal(ActualStateHeadline.ApplyTimeout, job.ActualStateHeadline);
        Assert.Equal(ServerSideStep.ApplyFromPlan, job.FailedOnServerSideStep);
        Assert.Equal("Runner stopped responding", job.ServerSideErrorHeader);
        Assert.Contains("Runner instance 'harness' is not connected.", job.ServerSideError);
    }

    [Theory]
    [InlineData("ApplyFromPlanPending", ServerSideStep.ApplyFromPlan)]
    [InlineData("DestroyFromPlanPending", ServerSideStep.DestroyFromPlan)]
    [InlineData("PlanPending", ServerSideStep.Plan)]
    [InlineData("PolicyValidatePending", ServerSideStep.PolicyValidate)]
    [InlineData("ValidatePending", ServerSideStep.Validate)]
    [InlineData("VariablesPending", ServerSideStep.Variables)]
    [InlineData("OutputPending", ServerSideStep.Output)]
    [InlineData("SplitPlanPending", ServerSideStep.SplitPlan)]
    [InlineData("SplitPlanEmptyVerifyPending", ServerSideStep.SplitPlanEmptyVerify)]
    [InlineData("SplitMigrateProvePending", ServerSideStep.SplitMigrateProve)]
    public void Step_Is_Determined_From_State_Name(string state, ServerSideStep expected)
    {
        Assert.Equal(expected, StepMapper.DetermineStepFromState(state));
    }
}
