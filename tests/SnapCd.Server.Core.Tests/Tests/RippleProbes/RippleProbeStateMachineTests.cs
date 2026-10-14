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
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
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
    public async Task A_request_creates_a_saga_carrying_what_the_probe_was_asked_for()
    {
        var jobId = await SeedJob();
        var inputs = """{"format_version":"1","variables":{"prefix_source":{"action":"unknown","type":"InN0cmluZyI=","before":"pGRlbW8=","after":"1AAA"}}}""";

        await Publish(jobId, inputs);

        // The shell has no step between start and finish, so the saga is created and finalized in
        // one transition and never exists to be read. What it carried is observable through the
        // event it publishes and the job row it closes.
        Assert.True(await WaitUntil(() => Completed(jobId)), "the probe should complete");

        var published = Assert.Single(_harness.Published.Select<RippleProbeCompleted>()
            .Where(m => m.Context.Message.ModuleJobId == jobId));

        Assert.Equal(_moduleId, published.Context.Message.ModuleId);
        Assert.Equal(_organizationId, published.Context.Message.OrganizationId);

        Assert.True(await _harness.Consumed.Any<RippleProbeRequested>(
            m => m.Context.Message.CorrelationId == jobId
                 && m.Context.Message.InputsJson == inputs));
    }

    [Fact]
    public async Task A_request_reaches_a_terminal_state_and_finalizes()
    {
        var jobId = await SeedJob();

        await Publish(jobId);

        Assert.True(await WaitUntil(() => Completed(jobId)), "the probe should reach Completed");

        Assert.True(_harness.Published.Select<RippleProbeCompleted>()
            .Any(m => m.Context.Message.ModuleJobId == jobId));

        await using var check = _fixture.CreateDbContext();
        var job = await check.RippleProbeJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);

        Assert.Equal(ExecutionStatus.Completed, job.Status);
        Assert.NotNull(job.TimestampEnd);

        // SetCompletedWhenFinalized plus Finalize means a finished probe leaves no saga row.
        Assert.False(await check.Set<RippleProbeSaga>().AsNoTracking()
            .AnyAsync(sa => sa.CorrelationId == jobId));
    }

    /// <summary>
    /// Two probes on one Module, each with its own saga. The saga is keyed on correlation id, so
    /// this is what proves the concurrency the entity's missing filtered index allows.
    /// </summary>
    [Fact]
    public async Task Two_probes_on_one_module_get_their_own_sagas()
    {
        var first = await SeedJob();
        var second = await SeedJob();

        await Publish(first);
        await Publish(second);

        Assert.True(await WaitUntil(() => Completed(first) && Completed(second)));
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
