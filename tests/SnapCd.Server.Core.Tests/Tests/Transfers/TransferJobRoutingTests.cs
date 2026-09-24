// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts.Enums;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Views;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Transfers;

/// <summary>
/// A transfer runs the same preamble steps as an ordinary job, on the same runner endpoints, so a
/// reply names only the job. Which family owns that job is resolved by looking it up, and getting
/// that wrong sends the reply to the wrong saga or nowhere.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransferJobRoutingTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _moduleId;
    private Guid _counterpartyId;
    private Guid _organizationId;
    private readonly List<Guid> _seededSagas = [];
    private readonly List<Guid> _seededTransfers = [];

    public TransferJobRoutingTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _counterpartyId = _fixture.Modules["0001"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.Set<TransferMigrateSaga>()
            .Where(s => _seededSagas.Contains(s.CorrelationId)).ExecuteDeleteAsync();
        await db.TransferRuns.Where(r => _seededTransfers.Contains(r.TransferId)).ExecuteDeleteAsync();
        await db.Transfers.Where(t => _seededTransfers.Contains(t.Id)).ExecuteDeleteAsync();
    }

    /// <summary>
    /// The reply carries only the job id, so the job has to be found by that alone. A transfer job
    /// resolving as Deployment would publish the reply as an ordinary step event and the transfer
    /// saga would never hear it.
    /// </summary>
    [Fact]
    public async Task A_Transfer_Job_Resolves_To_The_Transfer_Family()
    {
        var jobId = await SeedTransferSaga(nameof(ModuleJobSagaState.InitPending));

        var metaData = await Repository().GetSagaMetaData(jobId, _organizationId);

        Assert.Equal(JobSagaFamily.TransferMigrate, metaData.Family);
        Assert.Equal(nameof(ModuleJobSagaState.InitPending), metaData.CurrentState);
    }

    /// <summary>
    /// The preamble states are named in the deployment vocabulary, which is what lets the shared
    /// authorization path validate a transfer job without knowing it is one.
    /// </summary>
    [Fact]
    public async Task A_Transfer_Sagas_Preamble_States_Parse_As_Deployment_States()
    {
        var jobId = await SeedTransferSaga(nameof(ModuleJobSagaState.ValidatePending));

        var metaData = await Repository().GetSagaMetaData(jobId, _organizationId);

        Assert.Equal(
            ModuleJobSagaState.ValidatePending,
            Enum.Parse<ModuleJobSagaState>(metaData.CurrentState));
    }

    /// <summary>The runner pin travels with the metadata: a reply from another runner is refused.</summary>
    [Fact]
    public async Task The_Runner_Pin_Is_Carried_With_The_Job()
    {
        var jobId = await SeedTransferSaga(nameof(ModuleJobSagaState.PlanPending));

        var metaData = await Repository().GetSagaMetaData(jobId, _organizationId);

        Assert.Equal("runner-a", metaData.RunnerInstanceName);
        Assert.Equal(_organizationId, metaData.OrganizationId);
    }

    /// <summary>A job of no family at all is an error rather than a silently wrong route.</summary>
    [Fact]
    public async Task An_Unknown_Job_Is_Refused()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => Repository().GetSagaMetaData(Guid.NewGuid(), _organizationId));
    }

    private JobSagaRepository Repository() =>
        new JobSagaRepositoryFactory(new FixtureDbContextFactory(_fixture)).Create();

    private async Task<Guid> SeedTransferSaga(string state)
    {
        var transferId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        _seededTransfers.Add(transferId);
        _seededSagas.Add(jobId);

        await using var db = _fixture.CreateDbContext();

        db.Transfers.Add(new Transfer
        {
            Id = transferId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            CounterpartyModuleId = _counterpartyId,
            ConsentStatus = ConsentStatus.Granted
        });

        db.TransferRuns.Add(new TransferRun
        {
            Id = runId,
            OrganizationId = _organizationId,
            TransferId = transferId,
            Scope = TransferScope.Both,
            StartedAt = DateTimeOffset.UtcNow
        });

        db.Set<TransferMigrateSaga>().Add(new TransferMigrateSaga
        {
            CorrelationId = jobId,
            OrganizationId = _organizationId,
            TransferId = transferId,
            TransferRunId = runId,
            ModuleId = _moduleId,
            CurrentState = state,
            RunnerName = "runner",
            RunnerInstanceName = "runner-a",
            DeclaredJson = "{}"
        });

        await db.SaveChangesAsync();
        return jobId;
    }

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }
}
