// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.SplitMonolith;

/// <summary>
/// Runner callbacks are authorized against this repository. The deployment path looks only in the
/// apply and destroy tables, so without this a split job's callbacks would all be rejected.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class SplitMonolithSagaRepositoryTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _moduleId;
    private Guid _organizationId;
    private readonly List<Guid> _created = [];

    public SplitMonolithSagaRepositoryTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_created.Count == 0) return;

        await using var db = _fixture.CreateDbContext();
        var sagas = await db.Set<SplitMonolithSaga>().Where(s => _created.Contains(s.CorrelationId)).ToListAsync();
        db.Set<SplitMonolithSaga>().RemoveRange(sagas);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Reads_The_State_A_Callback_Is_Checked_Against()
    {
        var correlationId = await SeedSaga(SplitMonolithSagaState.MigrateProvePending.ToString());

        using var repository = Repo();
        var metaData = await repository.GetSagaMetaData(correlationId, _organizationId);

        Assert.Equal(SplitMonolithSagaState.MigrateProvePending.ToString(), metaData.CurrentState);
        Assert.Equal(_organizationId, metaData.OrganizationId);
    }

    /// <summary>The pinned instance is what ties every step of a split to one runner.</summary>
    [Fact]
    public async Task Carries_The_Pinned_Runner_Instance()
    {
        var correlationId = await SeedSaga(
            SplitMonolithSagaState.MigrateRunPending.ToString(),
            runnerInstanceName: "runner-a");

        using var repository = Repo();
        var metaData = await repository.GetSagaMetaData(correlationId, _organizationId);

        Assert.Equal("runner-a", metaData.RunnerInstanceName);
    }

    /// <summary>A callback arriving mid-cancellation is checked against the state it was dispatched from.</summary>
    [Fact]
    public async Task Carries_The_State_Held_Before_Cancelling()
    {
        var correlationId = await SeedSaga(
            SplitMonolithSagaState.CancellingImmediateKill.ToString(),
            previousStateBeforeCancelling: SplitMonolithSagaState.MigrateMapPending.ToString());

        using var repository = Repo();
        var metaData = await repository.GetSagaMetaData(correlationId, _organizationId);

        Assert.Equal(SplitMonolithSagaState.MigrateMapPending.ToString(), metaData.PreviousStateBeforeCancelling);
    }

    [Fact]
    public async Task Throws_For_A_Job_That_Is_Not_A_Split()
    {
        using var repository = Repo();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => repository.GetSagaMetaData(Guid.NewGuid(), _organizationId));
    }

    /// <summary>A saga is only visible to the organization that owns it.</summary>
    [Fact]
    public async Task Throws_For_Another_Organization()
    {
        var correlationId = await SeedSaga(SplitMonolithSagaState.PlanPending.ToString());

        using var repository = Repo();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => repository.GetSagaMetaData(correlationId, _fixture.Organizations["1"].Id));
    }

    private SplitMonolithSagaRepository Repo() => new(_fixture.CreateDbContext());

    private async Task<Guid> SeedSaga(
        string currentState,
        string? runnerInstanceName = null,
        string? previousStateBeforeCancelling = null)
    {
        var correlationId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.Set<SplitMonolithSaga>().Add(new SplitMonolithSaga
        {
            CorrelationId = correlationId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            CurrentState = currentState,
            RunnerId = _fixture.Runners["0"].Id,
            RunnerName = "runner",
            RunnerInstanceName = runnerInstanceName,
            PreviousStateBeforeCancelling = previousStateBeforeCancelling,
            DeclaredJson = "{}",
            RowVersion = []
        });
        await db.SaveChangesAsync();

        _created.Add(correlationId);
        return correlationId;
    }
}
