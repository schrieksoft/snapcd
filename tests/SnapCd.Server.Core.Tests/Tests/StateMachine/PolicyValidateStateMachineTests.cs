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
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Jobs;
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
    SnapCd.Server.Core.Events.Steps.DestroyCancelKillRequested,
    SnapCd.Server.Core.Events.Steps.DummyDestroyCancelKillCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyCancelKillCompleted>;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// MassTransit test-harness integration tests for the PolicyValidate step and PolicyDenied
/// terminal handling. Sagas are seeded directly at the state under test (EF saga repository
/// against the fixture database), then driven by publishing the step events.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class PolicyValidateStateMachineTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Module _module = null!;
    private Runner _runner = null!;

    public PolicyValidateStateMachineTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
            _runner = db.Runners.First(r => r.Id == _module.RunnerId);

            if (!db.RunnerConnections.Any(rc => rc.RunnerId == _runner.Id && rc.InstanceName == "harness"))
            {
                db.RunnerConnections.Add(new RunnerConnection
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _module.OrganizationId,
                    RunnerId = _runner.Id,
                    InstanceName = "harness",
                    SignalRConnectionId = "harness-connection",
                    ServerInstanceId = Guid.NewGuid()
                });
                await db.SaveChangesAsync();
            }
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

    private string DeclaredJson(List<ResolvedPolicy>? policies = null)
    {
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
            Policies = policies ?? new List<ResolvedPolicy>()
        };
        return JsonSerializer.Serialize(declared);
    }

    private static ResolvedPolicy InlinePolicy(PolicyEvaluateOn evaluateOn = PolicyEvaluateOn.ApplyAndDestroy)
    {
        return new ResolvedPolicy
        {
            Name = "p1",
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Terraform,
            Kind = PolicySourceKind.Inline,
            EvaluateOn = evaluateOn,
            PolicyContent = "package snapcd"
        };
    }

    private async Task<Guid> SeedJob(bool destroy, string state, List<ResolvedPolicy>? policies = null)
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

        var declaredJson = DeclaredJson(policies);
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
                DeclaredJson = declaredJson
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
                DeclaredJson = declaredJson
            });

        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task<string?> WaitForSagaState(Guid jobId, bool destroy, Func<string?, bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        string? state = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            state = destroy
                ? (await db.DestroyJobSagas.AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == jobId))?.CurrentState
                : (await db.ApplyJobSagas.AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == jobId))?.CurrentState;
            if (predicate(state)) return state;
            await Task.Delay(100);
        }
        return state;
    }

    private async Task<ModuleJob> GetJob(Guid jobId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId && j.OrganizationId == _module.OrganizationId);
    }

    [Fact]
    public async Task ApplyPlanCompleted_Without_Policies_Skips_PolicyValidate()
    {
        var jobId = await SeedJob(destroy: false, state: "PlanPending");

        await _harness.Bus.Publish(new ApplyPlanCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, TotalChangedCount = 1 });

        var state = await WaitForSagaState(jobId, false, s => s == "ApplyFromPlanPending" || s == "WaitingForApproval");
        Assert.True(state is "ApplyFromPlanPending" or "WaitingForApproval", $"Expected approval/apply path, got {state}");
        Assert.False(await _harness.Published.Any<ApplyPolicyValidateRequested>(x => x.Context.Message.CorrelationId == jobId));
    }

    [Fact]
    public async Task ApplyPlanCompleted_With_Policies_Dispatches_PolicyValidate()
    {
        var jobId = await SeedJob(destroy: false, state: "PlanPending", policies: [InlinePolicy()]);

        await _harness.Bus.Publish(new ApplyPlanCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, TotalChangedCount = 1 });

        var state = await WaitForSagaState(jobId, false, s => s == "PolicyValidatePending");
        Assert.Equal("PolicyValidatePending", state);
    }

    [Fact]
    public async Task Destroy_With_ApplyOnly_Policy_Skips_PolicyValidate()
    {
        var jobId = await SeedJob(destroy: true, state: "PlanPending", policies: [InlinePolicy(PolicyEvaluateOn.ApplyOnly)]);

        await _harness.Bus.Publish(new DestroyPlanCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, TotalChangedCount = 1 });

        // The generic machine's post-approval state is named ApplyFromPlanPending for both
        // instantiations (state properties are shared; the destroy saga dispatches
        // DestroyFromPlanRequested from it).
        var state = await WaitForSagaState(jobId, true, s => s == "ApplyFromPlanPending" || s == "WaitingForApproval");
        Assert.True(state is "ApplyFromPlanPending" or "WaitingForApproval", $"Expected approval/destroy path, got {state}");
        Assert.False(await _harness.Published.Any<DestroyPolicyValidateRequested>(x => x.Context.Message.CorrelationId == jobId));
    }

    [Fact]
    public async Task HardDenied_Finalizes_As_PolicyDenied_With_Reason()
    {
        var jobId = await SeedJob(destroy: false, state: "PolicyValidatePending", policies: [InlinePolicy()]);

        await _harness.Bus.Publish(new ApplyPolicyValidateCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, Outcome = PolicyOutcome.HardDenied });

        Assert.True(await _harness.Published.Any<ApplyModuleCancelled>(x =>
            x.Context.Message.ModuleJobId == jobId && x.Context.Message.CancellationReason == CancellationReason.PolicyDenied));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        ModuleJob job = await GetJob(jobId);
        while (job.Status != ExecutionStatus.PolicyDenied && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            job = await GetJob(jobId);
        }
        Assert.Equal(ExecutionStatus.PolicyDenied, job.Status);
        Assert.Equal(ActualStateHeadline.ApplyPolicyDenied, job.ActualStateHeadline);
        Assert.Equal(PolicyOutcome.HardDenied, job.PolicyOutcome);
    }

    [Fact]
    public async Task Destroy_HardDenied_Finalizes_As_PolicyDenied()
    {
        var jobId = await SeedJob(destroy: true, state: "PolicyValidatePending", policies: [InlinePolicy()]);

        await _harness.Bus.Publish(new DestroyPolicyValidateCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, Outcome = PolicyOutcome.HardDenied });

        Assert.True(await _harness.Published.Any<DestroyModuleCancelled>(x =>
            x.Context.Message.ModuleJobId == jobId && x.Context.Message.CancellationReason == CancellationReason.PolicyDenied));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        ModuleJob job = await GetJob(jobId);
        while (job.Status != ExecutionStatus.PolicyDenied && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            job = await GetJob(jobId);
        }
        Assert.Equal(ExecutionStatus.PolicyDenied, job.Status);
        Assert.Equal(ActualStateHeadline.DestroyPolicyDenied, job.ActualStateHeadline);
    }

    [Fact]
    public async Task SoftWarned_Continues_To_Approval_And_Persists_Outcome()
    {
        var jobId = await SeedJob(destroy: false, state: "PolicyValidatePending", policies: [InlinePolicy()]);

        await _harness.Bus.Publish(new ApplyPolicyValidateCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, Outcome = PolicyOutcome.SoftWarned });

        var state = await WaitForSagaState(jobId, false, s => s == "ApplyFromPlanPending" || s == "WaitingForApproval");
        Assert.True(state is "ApplyFromPlanPending" or "WaitingForApproval", $"Expected approval/apply path, got {state}");

        var job = await GetJob(jobId);
        Assert.Equal(PolicyOutcome.SoftWarned, job.PolicyOutcome);
        Assert.False(await _harness.Published.Any<ApplyModuleCancelled>(x => x.Context.Message.ModuleJobId == jobId));
    }

    [Fact]
    public async Task Faulted_Finalizes_As_Failed_Not_PolicyDenied()
    {
        var jobId = await SeedJob(destroy: false, state: "PolicyValidatePending", policies: [InlinePolicy()]);

        await _harness.Bus.Publish(new ApplyPolicyValidateFaulted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, ErrorMessage = "conftest not found" });

        Assert.True(await _harness.Published.Any<ApplyModuleFailed>(x => x.Context.Message.ModuleJobId == jobId));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        ModuleJob job = await GetJob(jobId);
        while (job.Status != ExecutionStatus.Failed && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            job = await GetJob(jobId);
        }
        Assert.Equal(ExecutionStatus.Failed, job.Status);
        Assert.NotEqual(ActualStateHeadline.ApplyPolicyDenied, job.ActualStateHeadline);
    }

    [Fact]
    public async Task Pulumi_ApplyPlanFaulted_With_HardDenied_Routes_To_PolicyDenied()
    {
        var jobId = await SeedJob(destroy: false, state: "PlanPending");

        await _harness.Bus.Publish(new ApplyPlanFaulted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, ErrorMessage = "preview failed", PolicyOutcome = PolicyOutcome.HardDenied });

        Assert.True(await _harness.Published.Any<ApplyModuleCancelled>(x =>
            x.Context.Message.ModuleJobId == jobId && x.Context.Message.CancellationReason == CancellationReason.PolicyDenied));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        ModuleJob job = await GetJob(jobId);
        while (job.Status != ExecutionStatus.PolicyDenied && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            job = await GetJob(jobId);
        }
        Assert.Equal(ExecutionStatus.PolicyDenied, job.Status);
        Assert.Equal(PolicyOutcome.HardDenied, job.PolicyOutcome);
    }

    [Fact]
    public async Task Pulumi_Only_Policies_Skip_PolicyValidate_Step()
    {
        // Pulumi policies are enforced inside the preview; the saga must take the fast path.
        var pulumiPolicy = new ResolvedPolicy
        {
            Name = "pack1",
            Scope = PolicyScope.Module,
            Engine = PolicyEngine.Pulumi,
            Kind = PolicySourceKind.Inline,
            EvaluateOn = PolicyEvaluateOn.ApplyAndDestroy,
            PolicyContent = "# pack"
        };
        var jobId = await SeedJob(destroy: false, state: "PlanPending", policies: [pulumiPolicy]);

        await _harness.Bus.Publish(new ApplyPlanCompleted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, TotalChangedCount = 1 });

        var state = await WaitForSagaState(jobId, false, s => s == "ApplyFromPlanPending" || s == "WaitingForApproval");
        Assert.True(state is "ApplyFromPlanPending" or "WaitingForApproval", $"Expected approval/apply path, got {state}");
        Assert.False(await _harness.Published.Any<ApplyPolicyValidateRequested>(x => x.Context.Message.CorrelationId == jobId));
    }

    [Fact]
    public async Task Pulumi_Destroy_DestroyPlanFaulted_HardDenied_Routes_To_PolicyDenied()
    {
        var jobId = await SeedJob(destroy: true, state: "PlanPending");

        await _harness.Bus.Publish(new DestroyPlanFaulted { CorrelationId = jobId, OrganizationId = _module.OrganizationId, ErrorMessage = "preview failed", PolicyOutcome = PolicyOutcome.HardDenied });

        Assert.True(await _harness.Published.Any<DestroyModuleCancelled>(x =>
            x.Context.Message.ModuleJobId == jobId && x.Context.Message.CancellationReason == CancellationReason.PolicyDenied));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        ModuleJob job = await GetJob(jobId);
        while (job.Status != ExecutionStatus.PolicyDenied && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            job = await GetJob(jobId);
        }
        Assert.Equal(ExecutionStatus.PolicyDenied, job.Status);
        Assert.Equal(ActualStateHeadline.DestroyPolicyDenied, job.ActualStateHeadline);
    }

    [Fact]
    public async Task Pulumi_ApplyPlanCompleted_SoftWarned_Persists_And_Continues()
    {
        var jobId = await SeedJob(destroy: false, state: "PlanPending");

        await _harness.Bus.Publish(new ApplyPlanCompleted
        {
            CorrelationId = jobId, OrganizationId = _module.OrganizationId,
            TotalChangedCount = 3, CreateCount = 1, ModifyCount = 1, DestroyCount = 1,
            PolicyOutcome = PolicyOutcome.SoftWarned
        });

        var state = await WaitForSagaState(jobId, false, s => s == "ApplyFromPlanPending" || s == "WaitingForApproval");
        Assert.True(state is "ApplyFromPlanPending" or "WaitingForApproval", $"Expected approval/apply path, got {state}");

        var job = await GetJob(jobId);
        Assert.Equal(PolicyOutcome.SoftWarned, job.PolicyOutcome);

        // Plan change counts are persisted for the approvals view.
        Assert.Equal(3, job.PlanTotalChangedCount);
        Assert.Equal(1, job.PlanCreateCount);
        Assert.Equal(1, job.PlanModifyCount);
        Assert.Equal(1, job.PlanDestroyCount);
    }
}
