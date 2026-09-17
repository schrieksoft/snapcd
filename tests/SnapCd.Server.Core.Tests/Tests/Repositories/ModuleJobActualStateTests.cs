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
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Repositories;

/// <summary>
/// A job ends once, by status rather than by end date, and the Module's displayed state follows
/// the newest job rather than the one whose row happened to be closed last.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ModuleJobActualStateTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _db = null!;
    private Guid _moduleId;
    private Guid _organizationId;
    private readonly List<Guid> _seeded = [];

    public ModuleJobActualStateTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _db = _fixture.CreateDbContext();
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.RemoveRange(db.ModuleJobs.Where(j => _seeded.Contains(j.Id)));
        await db.SaveChangesAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task A_Second_Finalize_Leaves_The_First_Untouched()
    {
        var jobId = await Seed(DateTimeOffset.UtcNow.AddMinutes(-10));
        var firstEnd = DateTimeOffset.UtcNow.AddMinutes(-5);

        await Repository().Finalize(jobId, _organizationId, ExecutionStatus.Completed, "first", firstEnd, actualStateHeadline: ActualStateHeadline.Applied);
        await Repository().Finalize(jobId, _organizationId, ExecutionStatus.Cancelled, "late cancel", DateTimeOffset.UtcNow, actualStateHeadline: ActualStateHeadline.ApplyCancelled);

        var job = await Load(jobId);
        Assert.Equal(ExecutionStatus.Completed, job.Status);
        Assert.Equal(ActualStateHeadline.Applied, job.ActualStateHeadline);
        Assert.Equal(firstEnd, job.TimestampEnd!.Value, TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task A_Server_Error_After_The_End_Leaves_The_Job_Untouched()
    {
        var jobId = await Seed(DateTimeOffset.UtcNow.AddMinutes(-10));
        var firstEnd = DateTimeOffset.UtcNow.AddMinutes(-5);

        await Repository().Finalize(jobId, _organizationId, ExecutionStatus.Completed, "first", firstEnd, actualStateHeadline: ActualStateHeadline.Applied);
        await Repository().FinalizeWithServerError(jobId, _organizationId, "late", DateTimeOffset.UtcNow, ActualStateHeadline.ApplyFailed, ServerSideStep.Start, "late error", "should not land");

        var job = await Load(jobId);
        Assert.Equal(ExecutionStatus.Completed, job.Status);
        Assert.Equal(ActualStateHeadline.Applied, job.ActualStateHeadline);
        Assert.Null(job.ServerSideErrorHeader);
    }

    [Fact]
    public async Task A_Running_Job_With_A_Stray_End_Date_Is_Still_Finalized()
    {
        var jobId = await Seed(DateTimeOffset.UtcNow.AddMinutes(-10), strayEnd: DateTimeOffset.UtcNow.AddMinutes(-9));

        await Repository().Finalize(jobId, _organizationId, ExecutionStatus.Cancelled, "cancel", DateTimeOffset.UtcNow, actualStateHeadline: ActualStateHeadline.ApplyCancelled);

        var job = await Load(jobId);
        Assert.Equal(ExecutionStatus.Cancelled, job.Status);
        Assert.Equal(ActualStateHeadline.ApplyCancelled, job.ActualStateHeadline);
        Assert.False(job.IsCurrent);
    }

    [Fact]
    public async Task A_Terminal_Status_Without_An_End_Date_Is_Still_Finalized()
    {
        var jobId = await Seed(DateTimeOffset.UtcNow.AddMinutes(-10), status: ExecutionStatus.Failed);

        await Repository().Finalize(jobId, _organizationId, ExecutionStatus.Cancelled, "cancel", DateTimeOffset.UtcNow, actualStateHeadline: ActualStateHeadline.ApplyCancelled);

        var job = await Load(jobId);
        Assert.Equal(ExecutionStatus.Cancelled, job.Status);
        Assert.NotNull(job.TimestampEnd);
    }

    [Fact]
    public async Task The_Newest_Job_Decides_The_State_Even_When_An_Older_One_Closed_Later()
    {
        var older = await Seed(DateTimeOffset.UtcNow.AddMinutes(-30));
        var newer = await Seed(DateTimeOffset.UtcNow.AddMinutes(-10));

        await Repository().Finalize(newer, _organizationId, ExecutionStatus.Completed, "apply", DateTimeOffset.UtcNow.AddMinutes(-9), actualStateHeadline: ActualStateHeadline.Applied);
        await Repository().Finalize(older, _organizationId, ExecutionStatus.Cancelled, "cancel", DateTimeOffset.UtcNow.AddMinutes(-8), actualStateHeadline: ActualStateHeadline.ApplyCancelled);

        var repo = Repository();
        Assert.Equal(ActualStateHeadline.Applied, await repo.GetCurrentActualStateHeadline(_moduleId, _organizationId));
        Assert.Equal([ActualStateHeadline.Applied, ActualStateHeadline.ApplyCancelled], await repo.GetRecentActualDefiniteRevisions(_moduleId, 2));
    }

    private ModuleJobRepository Repository()
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _organizationId);
        return new ModuleJobRepository(_fixture.CreateDbContext(), principalProvider, _fixture.CreateMockBus(), Options.Create(new ModuleJobRepositorySettings()));
    }

    private async Task<Guid> Seed(DateTimeOffset started, DateTimeOffset? strayEnd = null, ExecutionStatus status = ExecutionStatus.Running)
    {
        var jobId = Guid.NewGuid();
        _seeded.Add(jobId);
        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = started,
            TimestampEnd = strayEnd,
            JobType = "ApplyJobSaga",
            Status = status,
            IsCurrent = null
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task<ModuleJob> Load(Guid jobId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
    }
}
