// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Services;

/// <summary>
/// A module and its saga are written in one transaction, so a module with no saga row means the
/// row was lost. Without one the state machine correlates no events and the module is inert, with
/// no route back through the UI — so the saga is restored rather than reported missing.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ModuleSagaRecoveryTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private readonly List<Guid> _restored = [];

    public ModuleSagaRecoveryTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // The repository writes; leave the shared fixture as it was found.
        foreach (var id in _restored)
        {
            var saga = await _dbContext.Set<ModuleSaga>().FirstOrDefaultAsync(s => s.CorrelationId == id);
            if (saga != null) _dbContext.Set<ModuleSaga>().Remove(saga);
        }
        await _dbContext.SaveChangesAsync();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task RestoresTheSagaWhenTheModuleExists()
    {
        var moduleId = _fixture.Modules["0001"].Id;
        var orgId = _fixture.Organizations["0"].Id;
        await RemoveSagaIfPresent(moduleId);

        var saga = await Repo().Get(moduleId, orgId);

        Assert.Equal(moduleId, saga.CorrelationId);
        Assert.Equal(orgId, saga.OrganizationId);
        Assert.Equal("Gatekeeping", saga.CurrentState);
        _restored.Add(moduleId);

        // Persisted, not just returned — the next read has to find it.
        await using var verify = _fixture.CreateDbContext();
        Assert.NotNull(await verify.Set<ModuleSaga>().FirstOrDefaultAsync(s => s.CorrelationId == moduleId));
    }

    [Fact]
    public async Task RestoredSagaClaimsNoIntentForAModuleThatNeverRan()
    {
        // Module0001 has no ModuleJobs rows, so there is no last-known desired state to infer.
        var moduleId = _fixture.Modules["0001"].Id;
        await RemoveSagaIfPresent(moduleId);

        var saga = await Repo().Get(moduleId, _fixture.Organizations["0"].Id);
        _restored.Add(moduleId);

        Assert.Null(saga.DesiredStateHeadline);
        Assert.Null(saga.QueuedDesiredStateHeadline);
    }

    [Fact]
    public async Task RestoredSagaTakesItsDesiredStateFromTheLastCompletedJob()
    {
        // Module0000 has seeded ModuleJobs; a restored saga must not assert an intent that
        // contradicts what the module last did.
        var moduleId = _fixture.Modules["0000"].Id;
        var orgId = _fixture.Organizations["0"].Id;

        var job = await _dbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId && j.OrganizationId == orgId)
            .OrderByDescending(j => j.TimestampEnd)
            .FirstAsync();
        job.ActualStateHeadline = ActualStateHeadline.Destroyed;
        await _dbContext.SaveChangesAsync();

        await RemoveSagaIfPresent(moduleId);
        var saga = await Repo().Get(moduleId, orgId);
        _restored.Add(moduleId);

        Assert.Equal(DesiredStateHeadline.Destroyed, saga.DesiredStateHeadline);
    }

    [Fact]
    public async Task StillThrowsWhenTheModuleItselfIsAbsent()
    {
        // Nothing to restore a saga for; the caller's "does not exist" is correct here.
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => Repo().Get(Guid.NewGuid(), _fixture.Organizations["0"].Id));
    }

    private ModuleSagaRepository Repo() => new(_fixture.CreateDbContext());

    private async Task RemoveSagaIfPresent(Guid moduleId)
    {
        var existing = await _dbContext.Set<ModuleSaga>().FirstOrDefaultAsync(s => s.CorrelationId == moduleId);
        if (existing == null) return;
        _dbContext.Set<ModuleSaga>().Remove(existing);
        await _dbContext.SaveChangesAsync();
    }
}
