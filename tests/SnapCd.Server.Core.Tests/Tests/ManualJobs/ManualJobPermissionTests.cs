// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ManualJobs;

/// <summary>
/// Pause, start and decide all sit behind the Pause verb: Owners and Contributors may, Readers may
/// not. Each refusal is a PrincipalNotAuthorizedException, never a silent no-op.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ManualJobPermissionTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _moduleId;
    private Guid _organizationId;

    public ManualJobPermissionTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.Set<ModuleSaga>().FirstAsync(s => s.CorrelationId == _moduleId);
        saga.Paused = false;
        saga.PausedBy = null;
        saga.PausedAt = null;
        saga.PauseReason = null;
        db.ManualModuleJobs.RemoveRange(db.ManualModuleJobs.Where(j => j.ModuleId == _moduleId));
        await db.SaveChangesAsync();
    }

    private Guid Reader => _fixture.OrganizationPrincipals["0"][OrganizationRole.Reader].DirectUser.Id;
    private Guid Contributor => _fixture.OrganizationPrincipals["0"][OrganizationRole.Contributor].DirectUser.Id;
    private Guid Owner => _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id;

    [Fact]
    public async Task A_Reader_Cannot_Pause()
    {
        using var repo = SagaRepository(Reader);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.SetPaused(_moduleId, _organizationId, true, "test"));
    }

    [Fact]
    public async Task A_Contributor_Can_Pause_And_Resume()
    {
        using var repo = SagaRepository(Contributor);

        var paused = await repo.SetPaused(_moduleId, _organizationId, true, "test");
        Assert.True(paused.Paused);
        Assert.Equal(Contributor, paused.PausedBy);
        Assert.Equal("test", paused.PauseReason);

        var resumed = await repo.SetPaused(_moduleId, _organizationId, false, null);
        Assert.False(resumed.Paused);
        Assert.Null(resumed.PausedBy);
        Assert.Null(resumed.PauseReason);
    }

    [Fact]
    public async Task A_Reader_Cannot_Start_A_Manual_Job()
    {
        await SetPaused(true);
        using var service = Service(Reader);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => service.Start(_moduleId, _organizationId, ManualJobTypes.SplitMigrate));
    }

    [Fact]
    public async Task A_Contributor_Can_Start_A_Manual_Job()
    {
        await SetPaused(true);
        using var service = Service(Contributor);
        var job = await service.Start(_moduleId, _organizationId, ManualJobTypes.SplitMigrate);
        Assert.Equal(ExecutionStatus.Running, job.Status);
    }

    [Fact]
    public async Task A_Reader_Cannot_Decide()
    {
        await SetPaused(true);
        Guid jobId;
        using (var owner = Service(Owner))
            jobId = (await owner.Start(_moduleId, _organizationId, ManualJobTypes.SplitMigrate)).Id;

        using var reader = Service(Reader);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => reader.Decide(jobId, _moduleId, _organizationId, false));
    }

    [Fact]
    public async Task A_Contributor_Decides_Once()
    {
        await SetPaused(true);
        Guid jobId;
        using (var owner = Service(Owner))
            jobId = (await owner.Start(_moduleId, _organizationId, ManualJobTypes.SplitMigrate)).Id;
        await using (var db = _fixture.CreateDbContext())
        {
            (await db.ManualModuleJobs.SingleAsync(j => j.Id == jobId)).WaitingForApproval = true;
            await db.SaveChangesAsync();
        }

        using var contributor = Service(Contributor);
        await contributor.Decide(jobId, _moduleId, _organizationId, false);
        await Assert.ThrowsAsync<ManualJobNotAllowedException>(() => contributor.Decide(jobId, _moduleId, _organizationId, false));

        await using (var db = _fixture.CreateDbContext())
            Assert.Equal(1, await db.ManualModuleJobApprovals.CountAsync(a => a.ManualModuleJobId == jobId));
    }

    private ModuleSagaSecuredRepository SagaRepository(Guid principalId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        var secured = new ModuleSecuredRepository(
            new ModuleRepository(_fixture.CreateDbContext(), principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider);
        return new ModuleSagaSecuredRepository(new ModuleSagaRepository(_fixture.CreateDbContext(), _fixture.CreateMockBus()), secured);
    }

    private ManualJobService Service(Guid principalId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        var secured = new ModuleSecuredRepository(
            new ModuleRepository(_fixture.CreateDbContext(), principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider);
        return new ManualJobService(DbContextFactory(), secured, null, _fixture.CreateMockBus());
    }

    private IDbContextFactory<SnapCdDbContext> DbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
    }

    private async Task SetPaused(bool paused)
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.Set<ModuleSaga>().FirstAsync(s => s.CorrelationId == _moduleId);
        saga.Paused = paused;
        await db.SaveChangesAsync();
    }
}
