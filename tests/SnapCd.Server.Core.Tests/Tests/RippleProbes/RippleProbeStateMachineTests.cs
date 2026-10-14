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
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.RippleProbe;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.StateMachine.RippleProbes;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.RippleProbes;

/// <summary>
/// The saga shell: a request creates an instance, correlates to the job, and reaches a terminal
/// state. Registration, correlation and persistence are the three things that fail silently once
/// there are steps in between, so they are asserted on their own first.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class RippleProbeStateMachineTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private Guid _moduleId;
    private Guid _upstreamModuleId;
    private Guid _organizationId;
    private readonly List<Guid> _seeded = [];

    public RippleProbeStateMachineTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _upstreamModuleId = _fixture.Modules["0001"].Id;
        _organizationId = _fixture.Organizations["0"].Id;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IPrincipalProvider>(_ =>
            new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_organizationId]));
        services.AddScoped<IMaintenanceModeService, MaintenanceModeService>();
        services.AddScoped<RippleProbeJobRepository>();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<RippleProbeStateMachine, RippleProbeSaga>()
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
        await db.Set<RippleProbeSaga>().Where(s => _seeded.Contains(s.CorrelationId)).ExecuteDeleteAsync();
        await db.RippleProbeJobs.Where(j => _seeded.Contains(j.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_request_creates_a_saga_and_asks_for_a_runner()
    {
        var jobId = await SeedJob();
        var inputs = """{"format_version":"1","variables":{"prefix_source":{"action":"unknown","type":"InN0cmluZyI=","before":"pGRlbW8=","after":"1AAA"}}}""";

        await Publish(jobId, inputs);

        Assert.True(await WaitUntil(() => StateOf(jobId) == "RippleProbeSelectRunnerInstancePending"),
            "the probe should be waiting for a runner instance");

        await using var check = _fixture.CreateDbContext();
        var saga = await check.Set<RippleProbeSaga>().AsNoTracking()
            .SingleAsync(sa => sa.CorrelationId == jobId);

        Assert.Equal(_moduleId, saga.ModuleId);
        Assert.Equal(_organizationId, saga.OrganizationId);
        Assert.Equal(_upstreamModuleId, saga.UpstreamModuleId);
        Assert.Equal(inputs, saga.InputsJson);
        Assert.False(string.IsNullOrEmpty(saga.DeclaredJson));

        Assert.True(_harness.Published.Select<SelectRunnerInstanceRequested>()
            .Any(m => m.Context.Message.CorrelationId == jobId));
    }

    /// <summary>
    /// Each step hands off to the next. The saga is seeded into a step's pending state rather than
    /// driven there, because advancing through a step dispatches to a runner and the dispatch is
    /// not what these assertions are about. This is how SplitProveStateMachineTests does it.
    /// </summary>
    [Theory]
    [InlineData("RippleProbeGetModulePending", "RippleProbeInit")]
    [InlineData("RippleProbeInitPending", "RippleProbeValidate")]
    public async Task A_completed_step_hands_off_to_the_next(string state, string next)
    {
        var jobId = await Seed(state);

        await PublishCompletedFor(state, jobId);

        Assert.True(await WaitUntil(() => StateOf(jobId) is { } s
                                         && (s == $"{next}Pending" || s == $"{next}WaitingForRunner")),
            $"{state} should hand off to {next}; saga is in {StateOf(jobId)}");
    }

    [Fact]
    public async Task The_last_step_completes_the_probe()
    {
        var jobId = await Seed("RippleProbeValidatePending");

        await _harness.Bus.Publish(new RippleProbeValidateCompleted
            { CorrelationId = jobId, OrganizationId = _organizationId });

        Assert.True(await WaitUntil(() => Completed(jobId)),
            "Validate is the last step, so completing it should complete the probe");

        Assert.True(_harness.Published.Select<RippleProbeCompleted>()
            .Any(m => m.Context.Message.ModuleJobId == jobId));
    }

    [Theory]
    [InlineData("RippleProbeGetModulePending")]
    [InlineData("RippleProbeInitPending")]
    [InlineData("RippleProbeValidatePending")]
    public async Task A_faulted_step_fails_the_probe_and_records_the_error(string state)
    {
        var jobId = await Seed(state);

        await PublishFaultFor(state, jobId);

        Assert.True(await WaitUntil(() => StatusOf(jobId) == ExecutionStatus.Failed),
            $"a fault in {state} should fail the probe");

        Assert.True(_harness.Published.Select<RippleProbeFailed>()
            .Any(m => m.Context.Message.ModuleJobId == jobId));

        await using var check = _fixture.CreateDbContext();
        var job = await check.RippleProbeJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);

        Assert.NotNull(job.TimestampEnd);
        Assert.NotNull(job.ServerSideError);
    }

    [Theory]
    [InlineData("RippleProbeGetModulePending")]
    [InlineData("RippleProbeInitPending")]
    [InlineData("RippleProbeValidatePending")]
    public async Task A_cancelled_step_cancels_the_probe(string state)
    {
        var jobId = await Seed(state);

        await PublishCancelFor(state, jobId);

        Assert.True(await WaitUntil(() => StatusOf(jobId) == ExecutionStatus.Cancelled),
            $"a cancel in {state} should cancel the probe");

        Assert.True(_harness.Published.Select<RippleProbeCancelled>()
            .Any(m => m.Context.Message.ModuleJobId == jobId));
    }

    /// <summary>
    /// A cancel for a probe whose saga is gone still closes the job row. Without the missing
    /// instance fallback the cancel is swallowed and the row stays Running until the orphan sweep
    /// reports it as abandoned rather than cancelled.
    /// </summary>
    [Fact]
    public async Task A_cancel_with_no_saga_still_closes_the_job()
    {
        var jobId = await SeedJob();

        await _harness.Bus.Publish(new CancelRippleProbeJobRequested
        {
            CorrelationId = jobId,
            OrganizationId = _organizationId,
            CancellationType = CancellationType.ImmediateKill
        });

        Assert.True(await WaitUntil(() => StatusOf(jobId) == ExecutionStatus.Cancelled),
            "the job should be cancelled even with no saga to receive it");
    }

    /// <summary>
    /// Two probes on one Module, each with its own saga. The saga is keyed on correlation id, so
    /// this is what proves the concurrency the entity's missing filtered index allows.
    /// </summary>
    [Fact]
    public async Task Two_probes_on_one_module_are_tracked_separately()
    {
        var first = await Seed("RippleProbeInitPending");
        var second = await Seed("RippleProbeGetModulePending");

        Assert.Equal("RippleProbeInitPending", StateOf(first));
        Assert.Equal("RippleProbeGetModulePending", StateOf(second));

        await PublishFaultFor("RippleProbeInitPending", first);

        Assert.True(await WaitUntil(() => StatusOf(first) == ExecutionStatus.Failed));
        Assert.Equal(ExecutionStatus.Running, StatusOf(second));
        Assert.Equal("RippleProbeGetModulePending", StateOf(second));
    }

    // The operator-initiated cancel path, as distinct from a step reporting itself cancelled.

    [Theory]
    [InlineData("RippleProbeGetModulePending")]
    [InlineData("RippleProbeInitPending")]
    [InlineData("RippleProbeValidatePending")]
    public async Task A_cancel_kill_from_a_step_asks_the_runner_to_kill_and_parks_the_saga(string state)
    {
        var jobId = await Seed(state);

        await PublishCancel(jobId, CancellationType.ImmediateKill);

        Assert.True(await WaitUntil(() => StateOf(jobId) == "CancellingImmediateKill"),
            $"a cancel-kill in {state} should park the saga while the runner is asked to kill; saga is in {StateOf(jobId)}");

        Assert.True(_harness.Published.Select<RippleProbeCancelKillRequested>()
            .Any(m => m.Context.Message.CorrelationId == jobId),
            "the kill request should be dispatched to the runner");
    }

    [Fact]
    public async Task A_cancel_after_current_lets_the_running_step_finish_first()
    {
        var jobId = await Seed("RippleProbeInitPending");

        await PublishCancel(jobId, CancellationType.AfterCurrent);

        Assert.True(await WaitUntil(() => StateOf(jobId) == "CancellingAfterCurrent"),
            $"the saga should wait for the current step; saga is in {StateOf(jobId)}");

        // No kill goes out: the step is allowed to finish on its own.
        Assert.False(_harness.Published.Select<RippleProbeCancelKillRequested>()
            .Any(m => m.Context.Message.CorrelationId == jobId));

        // The step then reports, and that ends the job rather than continuing the chain.
        await _harness.Bus.Publish(new RippleProbeInitCompleted { CorrelationId = jobId, OrganizationId = _organizationId });

        Assert.True(await WaitUntil(() => StatusOf(jobId) == ExecutionStatus.Cancelled),
            "the step reporting back while cancelling should end the job");
    }

    [Fact]
    public async Task The_runner_answering_a_kill_ends_the_job()
    {
        var jobId = await Seed("CancellingImmediateKill");

        await _harness.Bus.Publish(new RippleProbeCancelKillCompleted
            { CorrelationId = jobId, OrganizationId = _organizationId });

        Assert.True(await WaitUntil(() => StatusOf(jobId) == ExecutionStatus.Cancelled));

        Assert.True(_harness.Published.Select<RippleProbeCancelled>()
            .Any(m => m.Context.Message.ModuleJobId == jobId));
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Failed")]
    [InlineData("Cancelled")]
    public async Task A_cancel_on_a_finished_probe_is_absorbed(string state)
    {
        var jobId = await Seed(state);

        await PublishCancel(jobId, CancellationType.ImmediateKill);

        Assert.Null(await ConsumedCancel(jobId));
    }

    /// <summary>
    /// Cancelling again while the timeout is still due does nothing: re-issuing would restart the
    /// clock, so repeated clicks would push the deadline out indefinitely.
    /// </summary>
    [Theory]
    [InlineData("CancellingImmediateKill")]
    [InlineData("CancellingAfterCurrent")]
    public async Task Cancelling_again_inside_the_window_changes_nothing(string state)
    {
        var jobId = await Seed(state, waitingSince: DateTime.UtcNow.AddSeconds(-5));

        await PublishCancel(jobId, CancellationType.ImmediateKill);

        Assert.Null(await ConsumedCancel(jobId));
        Assert.Equal(ExecutionStatus.Running, StatusOf(jobId));
        Assert.Equal(state, StateOf(jobId));
    }

    /// <summary>
    /// Cancelling again once the timeout was due means the timeout was lost: nothing else will
    /// ever end this job, so the click forces it closed.
    /// </summary>
    [Theory]
    [InlineData("CancellingImmediateKill")]
    [InlineData("CancellingAfterCurrent")]
    public async Task Cancelling_again_after_the_timeout_was_due_forces_it_closed(string state)
    {
        var jobId = await Seed(state, waitingSince: DateTime.UtcNow.AddMinutes(-10));

        await PublishCancel(jobId, CancellationType.ImmediateKill);

        Assert.True(await WaitUntil(() => StatusOf(jobId) == ExecutionStatus.Cancelled),
            "the stuck probe was not forced closed");

        Assert.True(_harness.Published.Select<RippleProbeCancelled>()
            .Any(m => m.Context.Message.ModuleJobId == jobId));
    }

    private Task PublishCancel(Guid jobId, CancellationType type) =>
        _harness.Bus.Publish(new CancelRippleProbeJobRequested
        {
            CorrelationId = jobId,
            OrganizationId = _organizationId,
            CancellationType = type
        });

    /// <summary>The consumer's exception, or null when it handled the message cleanly.</summary>
    private async Task<Exception?> ConsumedCancel(Guid jobId)
    {
        Assert.True(await WaitUntil(() =>
            _harness.Consumed.Select<CancelRippleProbeJobRequested>().Any(m => m.Context.Message.CorrelationId == jobId)),
            "the cancel was never consumed");

        return _harness.Consumed.Select<CancelRippleProbeJobRequested>()
            .First(m => m.Context.Message.CorrelationId == jobId).Exception;
    }

    /// <summary>Seeds a job and a saga already in the given state, as the split tests do.</summary>
    private async Task<Guid> Seed(string state, DateTime? waitingSince = null)
    {
        var jobId = await SeedJob();

        await using var db = _fixture.CreateDbContext();
        db.Set<RippleProbeSaga>().Add(new RippleProbeSaga
        {
            CorrelationId = jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            CurrentState = state,
            RunnerId = _fixture.Modules["0000"].RunnerId,
            RunnerName = "runner",
            RunnerInstanceName = "probe-test",
            DeclaredJson = "{}",
            UpstreamModuleId = _upstreamModuleId,
            InputsJson = "{}",
            ApprovalTimeoutMinutes = 0,
            WaitingSince = waitingSince,
            RowVersion = []
        });
        await db.SaveChangesAsync();

        return jobId;
    }

    private Task PublishCompletedFor(string state, Guid jobId) => state switch
    {
        "RippleProbeGetModulePending" => _harness.Bus.Publish(new RippleProbeGetModuleCompleted { CorrelationId = jobId, OrganizationId = _organizationId }),
        "RippleProbeInitPending" => _harness.Bus.Publish(new RippleProbeInitCompleted { CorrelationId = jobId, OrganizationId = _organizationId }),
        "RippleProbeValidatePending" => _harness.Bus.Publish(new RippleProbeValidateCompleted { CorrelationId = jobId, OrganizationId = _organizationId }),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    private Task PublishFaultFor(string state, Guid jobId) => state switch
    {
        "RippleProbeGetModulePending" => _harness.Bus.Publish(new RippleProbeGetModuleFaulted
            { CorrelationId = jobId, OrganizationId = _organizationId, ErrorMessage = "checkout failed", IsServerSideError = true }),
        "RippleProbeInitPending" => _harness.Bus.Publish(new RippleProbeInitFaulted
            { CorrelationId = jobId, OrganizationId = _organizationId, ErrorMessage = "init failed", IsServerSideError = true }),
        "RippleProbeValidatePending" => _harness.Bus.Publish(new RippleProbeValidateFaulted
            { CorrelationId = jobId, OrganizationId = _organizationId, ErrorMessage = "validate failed", IsServerSideError = true }),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    private Task PublishCancelFor(string state, Guid jobId) => state switch
    {
        "RippleProbeGetModulePending" => _harness.Bus.Publish(new RippleProbeGetModuleCancelled { CorrelationId = jobId, OrganizationId = _organizationId }),
        "RippleProbeInitPending" => _harness.Bus.Publish(new RippleProbeInitCancelled { CorrelationId = jobId, OrganizationId = _organizationId }),
        "RippleProbeValidatePending" => _harness.Bus.Publish(new RippleProbeValidateCancelled { CorrelationId = jobId, OrganizationId = _organizationId }),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    private string? StateOf(Guid jobId)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnapCdDbContext>();
        return db.Set<RippleProbeSaga>().AsNoTracking()
            .SingleOrDefault(sa => sa.CorrelationId == jobId)?.CurrentState;
    }

    private ExecutionStatus? StatusOf(Guid jobId)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnapCdDbContext>();
        return db.RippleProbeJobs.AsNoTracking()
            .Where(j => j.Id == jobId)
            .Select(j => (ExecutionStatus?)j.Status)
            .SingleOrDefault();
    }

    private bool Completed(Guid jobId)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnapCdDbContext>();
        return db.RippleProbeJobs.AsNoTracking()
            .Any(j => j.Id == jobId && j.Status == ExecutionStatus.Completed);
    }

    private async Task<Guid> SeedJob()
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.RippleProbeJobs.Add(new RippleProbeJob
        {
            Id = id,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            UpstreamModuleId = _upstreamModuleId,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            InputsJson = "{}"
        });
        await db.SaveChangesAsync();

        _seeded.Add(id);
        return id;
    }

    private Task Publish(Guid jobId, string inputsJson = "{}") =>
        _harness.Bus.Publish(new RippleProbeRequested
        {
            CorrelationId = jobId,
            OrganizationId = _organizationId,
            Declared = Declared(),
            UpstreamModuleId = _upstreamModuleId,
            InputsJson = inputsJson
        });

    private ResolvedModule Declared() => new()
    {
        ModuleId = _moduleId,
        NamespaceId = Guid.NewGuid(),
        StackId = Guid.NewGuid(),
        OrganizationId = _organizationId,
        RunnerId = _fixture.Modules["0000"].RunnerId,
        ModuleName = "consumer",
        NamespaceName = "ns",
        StackName = "stack",
        RunnerName = "runner",
        RunnerInstanceName = "runner-instance",
        SourceRevision = "main",
        SourceRevisionType = SourceRevisionType.Default,
        SourceUrl = "https://example.com/repo.git",
        SourceSubdirectory = "modules/consumer",
        Engine = "tofu",
        Policies = []
    };

    private static async Task<bool> WaitUntil(Func<bool> condition, int timeoutSeconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            await Task.Delay(100);
        }

        return false;
    }
}
