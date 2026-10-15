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
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Deletes;

/// <summary>
/// A Transfer names two Modules and restricts on both, so it cannot cascade: SQL Server refuses
/// two cascade paths into one table. Restrict is right for an open transfer, which a Module delete
/// should not silently resolve, but nothing cleared a transfer when its Module was deleted, so a
/// closed transfer blocked the delete permanently.
///
/// Each delete path needs its own test, because they do not share code: a namespace delete
/// cascades its Modules rather than deleting them through ModuleRepository, and a stack delete
/// cascades its namespaces. The same gap existed three times.
///
/// These build their own stack, namespace and modules rather than using the fixture's, because the
/// test deletes them and the fixture is shared across the collection.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransferBlocksDeleteTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _runnerId;
    private readonly List<Guid> _stacks = [];

    public TransferBlocksDeleteTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _runnerId = _fixture.Runners["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var namespaceIds = await db.Namespaces
            .Where(n => _stacks.Contains(n.StackId)).Select(n => n.Id).ToListAsync();
        var moduleIds = await db.Modules
            .Where(m => namespaceIds.Contains(m.NamespaceId)).Select(m => m.Id).ToListAsync();

        await db.StateMigrationJobs.Where(j => moduleIds.Contains(j.ModuleId)).ExecuteDeleteAsync();
        await db.Transfers.Where(t => moduleIds.Contains(t.ModuleId)).ExecuteDeleteAsync();

        // Modules first: both sagas now depend on it, so deleting the Module takes them. The
        // saga deletes that follow are for anything a test left behind without its Module.
        await db.Modules.Where(m => moduleIds.Contains(m.Id)).ExecuteDeleteAsync();
        await db.Set<ModuleSaga>().Where(s => moduleIds.Contains(s.CorrelationId)).ExecuteDeleteAsync();
        await db.Set<ModuleModifiedSaga>().Where(s => moduleIds.Contains(s.CorrelationId)).ExecuteDeleteAsync();

        await db.Namespaces.Where(n => namespaceIds.Contains(n.Id)).ExecuteDeleteAsync();
        await db.Stacks.Where(s => _stacks.Contains(s.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Module_In_A_Transfer_Deletes()
    {
        var (_, _, moduleId, counterpartyId) = await SeedHierarchy();
        await SeedTransfer(moduleId, counterpartyId, closed: true);

        await ModuleRepo().ExecuteDelete(moduleId, _organizationId);

        await AssertModuleAndSagasGone(moduleId);

        await using var db = _fixture.CreateDbContext();
        Assert.Empty(await db.Transfers.AsNoTracking().Where(t => t.ModuleId == moduleId).ToListAsync());
    }

    /// <summary>
    /// The counterparty side has its own foreign key, so deleting the other Module of the pair is
    /// a separate path through the same constraint.
    /// </summary>
    [Fact]
    public async Task The_Counterparty_Module_Of_A_Transfer_Deletes()
    {
        var (_, _, moduleId, counterpartyId) = await SeedHierarchy();
        await SeedTransfer(moduleId, counterpartyId, closed: true);

        await ModuleRepo().ExecuteDelete(counterpartyId, _organizationId);

        await AssertModuleAndSagasGone(counterpartyId);
    }

    /// <summary>
    /// A namespace delete cascades its Modules rather than going through ModuleRepository, so the
    /// clearing has to exist there too. This is the reported symptom.
    /// </summary>
    [Fact]
    public async Task A_Namespace_Holding_A_Transferred_Module_Deletes()
    {
        var (_, namespaceId, moduleId, counterpartyId) = await SeedHierarchy();
        await SeedTransfer(moduleId, counterpartyId, closed: true);

        await NamespaceRepo().ExecuteDelete(namespaceId, _organizationId);

        // Both Modules cascade with the namespace, so both sets of sagas go with them.
        await AssertModuleAndSagasGone(moduleId);
        await AssertModuleAndSagasGone(counterpartyId);

        await using var db = _fixture.CreateDbContext();
        Assert.Null(await db.Namespaces.AsNoTracking().FirstOrDefaultAsync(n => n.Id == namespaceId));
    }

    /// <summary>A stack delete cascades namespaces, so it reaches the same constraint again.</summary>
    [Fact]
    public async Task A_Stack_Holding_A_Transferred_Module_Deletes()
    {
        var (stackId, _, moduleId, counterpartyId) = await SeedHierarchy();
        await SeedTransfer(moduleId, counterpartyId, closed: true);

        await StackRepo().ExecuteDelete(stackId, _organizationId);

        await AssertModuleAndSagasGone(moduleId);
        await AssertModuleAndSagasGone(counterpartyId);

        await using var db = _fixture.CreateDbContext();
        Assert.Null(await db.Stacks.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stackId));
    }

    /// <summary>
    /// A transfer's jobs belong to the Module and cascade with it, but their foreign key to
    /// Transfers is NoAction, so they block the transfer delete unless detached first.
    /// </summary>
    [Fact]
    public async Task A_Transfer_With_Jobs_Does_Not_Block_The_Delete()
    {
        var (_, _, moduleId, counterpartyId) = await SeedHierarchy();
        var transferId = await SeedTransfer(moduleId, counterpartyId, closed: true);
        await SeedJob(moduleId, transferId);

        await ModuleRepo().ExecuteDelete(moduleId, _organizationId);

        await AssertModuleAndSagasGone(moduleId);

        await using var db = _fixture.CreateDbContext();
        Assert.Null(await db.Transfers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transferId));
    }

    /// <summary>
    /// An open transfer is cleared too, which is a behaviour change worth asserting rather than
    /// leaving to be discovered: Restrict used to make a Module in an open transfer undeletable,
    /// and now the delete proceeds.
    /// </summary>
    [Fact]
    public async Task An_Open_Transfer_Also_Does_Not_Block_The_Delete()
    {
        var (_, _, moduleId, counterpartyId) = await SeedHierarchy();
        await SeedTransfer(moduleId, counterpartyId, closed: false);

        await ModuleRepo().ExecuteDelete(moduleId, _organizationId);

        await AssertModuleAndSagasGone(moduleId);
    }

    [Fact]
    public async Task A_Module_With_No_Transfer_Still_Deletes()
    {
        var (_, _, moduleId, _) = await SeedHierarchy();

        await ModuleRepo().ExecuteDelete(moduleId, _organizationId);

        await AssertModuleAndSagasGone(moduleId);
    }

    /// <summary>
    /// A Module's two sagas are created with it in ModuleRepository.ExecuteCreate and are
    /// meaningless without it, so a delete has to take all three. ModuleSaga always did;
    /// ModuleModifiedSaga was mapped the other way round and was left behind.
    /// </summary>
    private async Task AssertModuleAndSagasGone(Guid moduleId)
    {
        await using var db = _fixture.CreateDbContext();

        Assert.Null(await db.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId));
        Assert.Null(await db.Set<ModuleSaga>().AsNoTracking()
            .FirstOrDefaultAsync(sa => sa.CorrelationId == moduleId));
        Assert.Null(await db.Set<ModuleModifiedSaga>().AsNoTracking()
            .FirstOrDefaultAsync(sa => sa.CorrelationId == moduleId));
    }

    private IPrincipalProvider Principal() =>
        _fixture.CreatePrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, _organizationId);

    private ModuleRepository ModuleRepo() =>
        new(_fixture.CreateDbContext(), Principal(), _fixture.CreateMockBus(), _fixture.CreateModuleSettings());

    private NamespaceRepository NamespaceRepo() =>
        new(_fixture.CreateDbContext(), Principal(), _fixture.CreateMockBus(), _fixture.CreateNamespaceSettings());

    private StackRepository StackRepo() =>
        new(_fixture.CreateDbContext(), Principal(), _fixture.CreateMockBus(),
            Options.Create(new StackRepositorySettings()));

    private async Task<(Guid StackId, Guid NamespaceId, Guid ModuleId, Guid CounterpartyId)> SeedHierarchy()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var stack = new Stack
        {
            Id = Guid.NewGuid(), Name = $"xfer-stack-{suffix}",
            OrganizationId = _organizationId, CreatedDateTime = DateTime.UtcNow
        };
        _stacks.Add(stack.Id);

        var ns = new Namespace
        {
            Id = Guid.NewGuid(), Name = $"xfer-ns-{suffix}", StackId = stack.Id,
            OrganizationId = _organizationId, CreatedDateTime = DateTime.UtcNow
        };

        var module = NewModule($"xfer-a-{suffix}", ns.Id);
        var counterparty = NewModule($"xfer-b-{suffix}", ns.Id);

        await using var db = _fixture.CreateDbContext();
        db.Stacks.Add(stack);
        db.Namespaces.Add(ns);
        db.Modules.AddRange(module, counterparty);
        await db.SaveChangesAsync();

        return (stack.Id, ns.Id, module.Id, counterparty.Id);
    }

    private Module NewModule(string name, Guid namespaceId)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(), Name = name, NamespaceId = namespaceId, RunnerId = _runnerId,
            OrganizationId = _organizationId, SourceUrl = $"https://example.com/{name}",
            SourceRevision = "main", SourceSubdirectory = "terraform",
            CreatedDateTime = DateTime.UtcNow
        };
        module.ModuleSaga = new ModuleSaga
        {
            CorrelationId = module.Id, OrganizationId = _organizationId, RowVersion = [],
            CurrentState = nameof(ModuleStateMachine.Gatekeeping),
            DesiredStateHeadline = DesiredStateHeadline.Applied
        };
        module.ModuleModifiedSaga = new ModuleModifiedSaga
        {
            CorrelationId = module.Id, OrganizationId = _organizationId, RowVersion = [],
            CurrentState = nameof(ModuleModifiedStateMachine.Idle)
        };
        return module;
    }

    private async Task<Guid> SeedTransfer(Guid moduleId, Guid counterpartyModuleId, bool closed)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.Transfers.Add(new Transfer
        {
            Id = id,
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            CounterpartyModuleId = counterpartyModuleId,
            ConsentStatus = closed ? ConsentStatus.Granted : ConsentStatus.Pending,
            ClosedAt = closed ? DateTimeOffset.UtcNow : null,
            CloseReason = closed ? "a module finished" : null
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task SeedJob(Guid moduleId, Guid transferId)
    {
        await using var db = _fixture.CreateDbContext();
        db.StateMigrationJobs.Add(new StateMigrationJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            TransferId = transferId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.TransferMigrate,
            Status = ExecutionStatus.Completed
        });
        await db.SaveChangesAsync();
    }
}
