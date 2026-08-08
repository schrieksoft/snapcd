// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class QuiescenceProbeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;

    private Module _module = null!;
    private Runner _runner = null!;

    public QuiescenceProbeTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
        _runner = db.Runners.First(r => r.Id == _module.RunnerId);

        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        _provider = services.BuildServiceProvider(true);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    private QuiescenceProbeService CreateService()
    {
        var factory = _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
        return new QuiescenceProbeService(factory, new StuckJobDetectionService(factory, Options.Create(new StuckJobDetectionSettings())));
    }

    private async Task<Guid> SeedSaga(string state, DateTime? waitingSince = null)
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
            JobType = "Apply",
            IsCurrent = null
        });
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
            Policies = new List<ResolvedPolicy>()
        };
        db.ApplyJobSagas.Add(new ApplyJobSaga
        {
            CorrelationId = jobId,
            CurrentState = state,
            ModuleId = _module.Id,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            RunnerName = _runner.Name,
            RunnerInstanceName = "probe-harness",
            DeclaredJson = JsonSerializer.Serialize(declared),
            WaitingSince = waitingSince
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    [Fact]
    public async Task Counts_Every_Category_And_Reports_The_Database_Gate()
    {
        var service = CreateService();
        var before = await service.GetDrainStatusAsync();

        var pending = await SeedSaga("PlanPending");
        var parkedStale = await SeedSaga("ApplyFromPlanWaitingForRunner", DateTime.UtcNow.AddHours(-2));
        var approval = await SeedSaga("WaitingForApproval", DateTime.UtcNow.AddMinutes(-5));
        var cancelling = await SeedSaga("CancellingImmediateKill", DateTime.UtcNow.AddMinutes(-1));

        Guid runId;
        DesiredStateHeadline? originalQueued;
        QueuedReason? originalReason;
        await using (var db = _fixture.CreateDbContext())
        {
            var moduleSaga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _module.Id);
            if (moduleSaga == null)
            {
                moduleSaga = new ModuleSaga { CorrelationId = _module.Id, OrganizationId = _module.OrganizationId, CurrentState = "Gatekeeping" };
                db.ModuleSagas.Add(moduleSaga);
            }

            originalQueued = moduleSaga.QueuedDesiredStateHeadline;
            originalReason = moduleSaga.QueuedReason;
            moduleSaga.QueuedDesiredStateHeadline = DesiredStateHeadline.Applied;
            moduleSaga.QueuedReason = QueuedReason.Maintenance;

            var jobId = Guid.NewGuid();
            db.ModuleJobs.Add(new ModuleJob { Id = jobId, OrganizationId = _module.OrganizationId, ModuleId = _module.Id, TimestampStart = DateTimeOffset.UtcNow, Status = ExecutionStatus.Running, JobType = "Apply", IsCurrent = null });
            var missionId = Guid.NewGuid();
            db.ModuleJobMissions.Add(new ModuleJobMission { Id = missionId, OrganizationId = _module.OrganizationId, ModuleJobId = jobId, MissionId = Guid.NewGuid(), AgentId = Guid.NewGuid(), MissionType = MissionType.AutoDiagnose, Status = MissionStatus.Running });
            runId = Guid.NewGuid();
            db.ModuleJobMissionRuns.Add(new ModuleJobMissionRun { Id = runId, OrganizationId = _module.OrganizationId, ModuleJobMissionId = missionId, ModuleJobId = jobId, MissionType = MissionType.AutoDiagnose, AgentId = Guid.NewGuid(), InvocationId = Guid.NewGuid(), AttemptNumber = 1, Status = MissionStatus.Running, DeadlineAt = DateTime.UtcNow.AddMinutes(30) });
            await db.SaveChangesAsync();
        }

        try
        {
            var after = await service.GetDrainStatusAsync();

            Assert.Equal(before.PendingJobs + 1, after.PendingJobs);
            Assert.Equal(before.ParkedJobs + 1, after.ParkedJobs);
            Assert.Equal(before.AwaitingApproval + 1, after.AwaitingApproval);
            Assert.Equal(before.Cancelling + 1, after.Cancelling);
            Assert.Equal(before.QueuedForMaintenance + 1, after.QueuedForMaintenance);
            Assert.Equal(before.ActiveMissions + 1, after.ActiveMissions);
            Assert.False(after.IsDatabaseQuiet);
            Assert.Contains(after.StuckJobs, s => s.JobId == parkedStale);
            Assert.DoesNotContain(after.StuckJobs, s => s.JobId == pending);
        }
        finally
        {
            await using var db = _fixture.CreateDbContext();
            var moduleSaga = await db.ModuleSagas.SingleAsync(s => s.CorrelationId == _module.Id);
            moduleSaga.QueuedDesiredStateHeadline = originalQueued;
            moduleSaga.QueuedReason = originalReason;
            var run = await db.ModuleJobMissionRuns.SingleAsync(r => r.Id == runId);
            run.Status = MissionStatus.Cancelled;
            var sagas = await db.ApplyJobSagas.Where(s => new[] { pending, parkedStale, approval, cancelling }.Contains(s.CorrelationId)).ToListAsync();
            db.ApplyJobSagas.RemoveRange(sagas);
            await db.SaveChangesAsync();
        }
    }
}
