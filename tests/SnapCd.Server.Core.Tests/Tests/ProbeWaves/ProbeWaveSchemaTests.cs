// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.Utils.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// What the schema promises, asserted against the database rather than read off the class maps:
/// a Module can probe more than once in a wave, an edge exists once per consumer and upstream
/// pair, the signal hierarchy round-trips through one table, the status persists as text, and a
/// wave owns the sets produced in it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ProbeWaveSchemaTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _originModuleId;
    private Guid _consumerModuleId;
    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _originJobs = [];
    private readonly List<Guid> _signalSets = [];
    private readonly List<Guid> _resourceChangeSets = [];

    public ProbeWaveSchemaTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _originModuleId = _fixture.Modules["0000"].Id;
        _consumerModuleId = _fixture.Modules["0001"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.SignalEdges.Where(e => _waves.Contains(e.ProbeWaveId)).ExecuteDeleteAsync();
        await db.ModuleProbeAttempts.Where(p => _waves.Contains(p.ProbeWaveId)).ExecuteDeleteAsync();
        await db.SignalOutputs.Where(o => _signalSets.Contains(o.SignalOutputSetId)).ExecuteDeleteAsync();
        await db.SignalOutputSets.Where(s => _signalSets.Contains(s.Id)).ExecuteDeleteAsync();
        await db.ResourceChangeSets.Where(s => _resourceChangeSets.Contains(s.Id)).ExecuteDeleteAsync();
        await db.ProbeWaves.Where(w => _waves.Contains(w.Id)).ExecuteDeleteAsync();
        await db.ModuleJobs.Where(j => _originJobs.Contains(j.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Module_Probes_In_A_Wave_More_Than_Once()
    {
        var waveId = await SeedWave();

        // A probe is repeatable: a Module that failed is probed again by a later Apply, and each
        // attempt keeps its own row.
        await SeedAttempt(waveId, _consumerModuleId);
        await SeedAttempt(waveId, _consumerModuleId);

        await using var db = _fixture.CreateDbContext();
        var attempts = await db.ModuleProbeAttempts
            .CountAsync(a => a.ProbeWaveId == waveId && a.ModuleId == _consumerModuleId);

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task The_Same_Module_Probes_In_Two_Waves()
    {
        var first = await SeedWave();
        var second = await SeedWave();

        await SeedAttempt(first, _consumerModuleId);
        await SeedAttempt(second, _consumerModuleId);
    }

    [Fact]
    public async Task An_Edge_Exists_Once_Per_Consumer_And_Upstream()
    {
        var waveId = await SeedWave();

        await SeedEdge(waveId, _consumerModuleId, _originModuleId);

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => SeedEdge(waveId, _consumerModuleId, _originModuleId));
    }

    /// <summary>
    /// The diamond's shape: one consumer waiting on two upstreams is two rows, which is what makes
    /// "both have resolved" expressible at all.
    /// </summary>
    [Fact]
    public async Task One_Consumer_Takes_An_Edge_Per_Upstream()
    {
        var waveId = await SeedWave();

        await SeedEdge(waveId, _consumerModuleId, _originModuleId);
        await SeedEdge(waveId, _consumerModuleId, _fixture.Modules["0002"].Id);

        await using var db = _fixture.CreateDbContext();
        var edges = await db.SignalEdges.AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId && e.ConsumerModuleId == _consumerModuleId)
            .ToListAsync();

        Assert.Equal(2, edges.Count);
        Assert.All(edges, e => Assert.Equal(SignalEdgeStatus.Potential, e.Status));
    }

    [Fact]
    public async Task An_Edge_Status_Persists_As_Text()
    {
        var waveId = await SeedWave();
        var edgeId = await SeedEdge(
            waveId, _consumerModuleId, _originModuleId, SignalEdgeStatus.NotNeeded);

        await using var db = _fixture.CreateDbContext();
        var stored = await db.Database
            .SqlQuery<string>($"SELECT [Status] AS [Value] FROM SignalEdges WHERE Id = {edgeId}")
            .SingleAsync();

        Assert.Equal("NotNeeded", stored);
    }

    /// <summary>
    /// A sensitive signal and a plain one share a table, so the discriminator is what keeps them
    /// apart. Reading each back through its own set proves the mapping rather than the class map.
    /// </summary>
    [Fact]
    public async Task Both_Kinds_Of_Signal_Round_Trip_Through_One_Table()
    {
        var waveId = await SeedWave();
        var setId = await SeedSignalSet(waveId);

        const string signalJson =
            """{"action":"unknown","type":"InN0cmluZyI=","before":"sGNoYW1waW9uLWhlcnJpbmc=","after":"1AAA"}""";

        await using (var db = _fixture.CreateDbContext())
        {
            db.LiteralSignalOutputs.Add(new LiteralSignalOutput
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                SignalOutputSetId = setId,
                Name = "pet_id",
                Sensitive = false,
                SignalJson = signalJson
            });
            db.SecretSignalOutputs.Add(new SecretSignalOutput
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                SignalOutputSetId = setId,
                Name = "db_password",
                Sensitive = true,
                RemoteSecretName = "signal-db-password"
            });
            await db.SaveChangesAsync();
        }

        await using var check = _fixture.CreateDbContext();

        var literal = await check.LiteralSignalOutputs.AsNoTracking()
            .SingleAsync(o => o.SignalOutputSetId == setId);
        Assert.Equal(signalJson, literal.SignalJson);
        Assert.False(literal.Sensitive);

        var secret = await check.SecretSignalOutputs.AsNoTracking()
            .SingleAsync(o => o.SignalOutputSetId == setId);
        Assert.Equal("signal-db-password", secret.RemoteSecretName);
        Assert.True(secret.Sensitive);

        // The base set sees both, which is what one table means.
        Assert.Equal(2, await check.SignalOutputs.AsNoTracking().CountAsync(o => o.SignalOutputSetId == setId));
    }

    [Fact]
    public async Task An_Output_Name_Appears_Once_Per_Signal_Set()
    {
        var waveId = await SeedWave();
        var setId = await SeedSignalSet(waveId);

        await SeedSignal(setId, "pet_id");

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => SeedSignal(setId, "pet_id"));
    }

    /// <summary>
    /// A wave owns what its probes produced, so deleting it takes the sets with it. The alternative
    /// leaves rows naming a wave that no longer exists, which nothing can then attribute or reclaim.
    /// </summary>
    [Fact]
    public async Task Deleting_A_Wave_Takes_Its_Signal_Sets()
    {
        var waveId = await SeedWave();
        var setId = await SeedSignalSet(waveId);
        await SeedSignal(setId, "pet_id");

        await DeleteWave(waveId);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.SignalOutputSets.AnyAsync(s => s.Id == setId));
        // The signals cascade from the set, so they go too.
        Assert.False(await db.SignalOutputs.AnyAsync(o => o.SignalOutputSetId == setId));
    }

    [Fact]
    public async Task Deleting_A_Wave_Takes_Its_Resource_Change_Sets()
    {
        var waveId = await SeedWave();
        var setId = await SeedResourceChangeSet(waveId);

        await DeleteWave(waveId);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.ResourceChangeSets.AnyAsync(s => s.Id == setId));
    }

    /// <summary>
    /// The attempt pointing at a set is cascaded by the same wave, so the whole episode goes in one
    /// delete: nothing is left holding a reference and nothing has to be ordered by hand.
    /// </summary>
    [Fact]
    public async Task Deleting_A_Wave_Takes_The_Attempt_That_Recorded_A_Set()
    {
        var waveId = await SeedWave();
        var setId = await SeedSignalSet(waveId);
        var changeSetId = await SeedResourceChangeSet(waveId);
        var attemptId = await SeedAttempt(waveId, _consumerModuleId, setId, changeSetId);

        await DeleteWave(waveId);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.ModuleProbeAttempts.AnyAsync(a => a.Id == attemptId));
        Assert.False(await db.SignalOutputSets.AnyAsync(s => s.Id == setId));
        Assert.False(await db.ResourceChangeSets.AnyAsync(s => s.Id == changeSetId));
    }

    /// <summary>
    /// A set cannot name a wave that does not exist. This is the invariant the ownership is for:
    /// without it a set outlives its wave and nothing left in the database says what it answered.
    /// </summary>
    [Fact]
    public async Task A_Signal_Set_Cannot_Name_A_Wave_That_Does_Not_Exist()
    {
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => SeedSignalSet(Guid.NewGuid()));
    }

    [Fact]
    public async Task A_Resource_Change_Set_Cannot_Name_A_Wave_That_Does_Not_Exist()
    {
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => SeedResourceChangeSet(Guid.NewGuid()));
    }

    /// <summary>
    /// A Module reaches these sets twice, directly and through the job owning the wave, so its own
    /// path restricts rather than cascades and ModuleRepository clears the rows itself. Asserted
    /// here because the restriction is invisible from the delete that relies on it.
    /// </summary>
    [Fact]
    public async Task A_Module_Does_Not_Cascade_Its_Signal_Sets()
    {
        var waveId = await SeedWave();
        var setId = await SeedSignalSet(waveId);

        await using var db = _fixture.CreateDbContext();
        var behaviour = db.Model
            .FindEntityType(typeof(SignalOutputSet))!
            .GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Module))
            .DeleteBehavior;

        Assert.Equal(DeleteBehavior.Restrict, behaviour);
        Assert.True(await db.SignalOutputSets.AnyAsync(s => s.Id == setId));
    }

    /// <summary>
    /// A participant's job is not the wave's origin, so deleting it leaves the wave standing. What
    /// it contributed goes with it: the job is named in a column that cannot cascade, because the
    /// job already reaches those tables through the wave it owns and SQL Server allows one such
    /// path, so the repository clears them.
    ///
    /// The wave then has no attempt for that Module, which is what "has not answered" already
    /// means to every reader of one.
    /// </summary>
    [Fact]
    public async Task Deleting_A_Participants_Job_Takes_Its_Contribution_And_Leaves_The_Wave()
    {
        var waveId = await SeedWave();
        var participantJobId = await AddOriginJob(_consumerModuleId);

        var setId = await SeedSignalSet(waveId, participantJobId);
        var changeSetId = await SeedResourceChangeSet(waveId, participantJobId);
        var attemptId = await SeedAttempt(waveId, _consumerModuleId, setId, changeSetId, participantJobId);

        using var repository = new ModuleJobRepository(
            _fixture.CreateDbContext(),
            new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_organizationId]),
            _fixture.CreateMockBus(),
            Options.Create(new ModuleJobRepositorySettings()));

        await repository.Delete(participantJobId, _organizationId);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.ModuleJobs.AnyAsync(j => j.Id == participantJobId));
        Assert.False(await db.ModuleProbeAttempts.AnyAsync(a => a.Id == attemptId));
        Assert.False(await db.SignalOutputSets.AnyAsync(x => x.Id == setId));
        Assert.False(await db.ResourceChangeSets.AnyAsync(x => x.Id == changeSetId));

        // The wave belongs to a different job, so it is untouched.
        Assert.True(await db.ProbeWaves.AnyAsync(w => w.Id == waveId));
    }

    private async Task DeleteWave(Guid waveId)
    {
        await using var db = _fixture.CreateDbContext();
        await db.ProbeWaves.Where(w => w.Id == waveId).ExecuteDeleteAsync();
    }

    private async Task<Guid> SeedResourceChangeSet(Guid waveId, Guid? moduleJobId = null)
    {
        var id = Guid.NewGuid();
        _resourceChangeSets.Add(id);

        await using var db = _fixture.CreateDbContext();
        db.ResourceChangeSets.Add(new ResourceChangeSet
        {
            Id = id,
            OrganizationId = _organizationId,
            ModuleId = _originModuleId,
            ProbeWaveId = waveId,
            ModuleJobId = moduleJobId ?? await AddOriginJob(_originModuleId),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            TotalCount = 1
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedWave()
    {
        var id = Guid.NewGuid();
        _waves.Add(id);

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(new ProbeWave
        {
            Id = id,
            OrganizationId = _organizationId,
            OriginModuleJobId = await AddOriginJob(_originModuleId)
        });
        await db.SaveChangesAsync();
        return id;
    }


    /// <summary>
    /// The origin job whose approval a wave informs, and which it is constrained to. The job is
    /// awaiting that decision, which is the state a wave is live in.
    /// </summary>
    private async Task<Guid> AddOriginJob(Guid moduleId)
    {
        var jobId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            JobType = "ApplyJobSaga",
            Status = ExecutionStatus.Running,
            WaitingForApproval = true,
            TimestampStart = DateTimeOffset.UtcNow.AddTicks(_originJobs.Count),
            CreatedDateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _originJobs.Add(jobId);
        return jobId;
    }

    private async Task<Guid> SeedAttempt(
        Guid waveId,
        Guid moduleId,
        Guid? signalOutputSetId = null,
        Guid? resourceChangeSetId = null,
        Guid? probeModuleJobId = null)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = id,
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId,
            SignalOutputSetId = signalOutputSetId,
            ResourceChangeSetId = resourceChangeSetId,
            ProbeModuleJobId = probeModuleJobId
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedEdge(
        Guid waveId,
        Guid consumerModuleId,
        Guid upstreamModuleId,
        SignalEdgeStatus status = SignalEdgeStatus.Potential)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.SignalEdges.Add(new SignalEdge
        {
            Id = id,
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ConsumerModuleId = consumerModuleId,
            UpstreamModuleId = upstreamModuleId,
            Status = status
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedSignalSet(Guid waveId, Guid? moduleJobId = null)
    {
        var id = Guid.NewGuid();
        _signalSets.Add(id);

        await using var db = _fixture.CreateDbContext();
        db.SignalOutputSets.Add(new SignalOutputSet
        {
            Id = id,
            OrganizationId = _organizationId,
            ModuleId = _originModuleId,
            ProbeWaveId = waveId,
            ModuleJobId = moduleJobId ?? await AddOriginJob(_originModuleId),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Checksum = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task SeedSignal(Guid setId, string name)
    {
        await using var db = _fixture.CreateDbContext();
        db.LiteralSignalOutputs.Add(new LiteralSignalOutput
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            SignalOutputSetId = setId,
            Name = name,
            Sensitive = false,
            SignalJson = "{}"
        });
        await db.SaveChangesAsync();
    }
}
