// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.Smoke;

/// <summary>
/// Tier A smoke for OutputModuleSupply, which hangs off the producing Module and takes its
/// permissions from GenericModuleChildSecuredRepository.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OutputModuleSupply_SmokeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public OutputModuleSupply_SmokeTests(Fixture fixture) { _fixture = fixture; }

    public Task InitializeAsync() { _dbContext = _fixture.CreateDbContext(); return Task.CompletedTask; }
    public Task DisposeAsync() { _dbContext?.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_OwnerCanRead()
    {
        var target = Seeded("UpdateCan");
        var repo = Repo(OwnerId());
        var supply = await repo.Get(target.Id, _fixture.Organizations["0"].Id);
        Assert.NotNull(supply);
    }

    [Fact]
    public async Task Get_NoRoleCannotRead()
    {
        var target = Seeded("UpdateCan");
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Get(target.Id, _fixture.Organizations["0"].Id));
    }

    [Fact]
    public async Task List_OwnerSeesSupply()
    {
        var target = Seeded("UpdateCan");
        var repo = Repo(OwnerId());
        var items = await repo.List(_fixture.Organizations["0"].Id);
        Assert.Contains(items, x => x.Id == target.Id);
    }

    [Fact]
    public async Task List_NoRoleSeesNothing()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        var items = await repo.List(_fixture.Organizations["0"].Id);
        Assert.Empty(items);
    }

    [Fact]
    public async Task Create_OwnerCanCreate()
    {
        var repo = Repo(OwnerId());
        var supply = Build("create_allowed");
        await repo.Create(supply);
        Assert.NotEqual(Guid.Empty, supply.Id);
    }

    [Fact]
    public async Task Create_NoRoleCannotCreate()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        var supply = Build("create_denied");
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Create(supply));
    }

    [Fact]
    public async Task Update_OwnerCanUpdate()
    {
        var target = Seeded("UpdateCan");
        target.OutputName = $"smoke_update_can_{Guid.NewGuid():N}";
        var repo = Repo(OwnerId());
        await repo.Update(target);
    }

    [Fact]
    public async Task Update_NoRoleCannotUpdate()
    {
        var target = Seeded("UpdateCan");
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Update(target));
    }

    [Fact]
    public async Task Delete_OwnerCanDelete()
    {
        var target = Seeded("DeleteCan");
        var repo = Repo(OwnerId());
        await repo.Delete(target.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Delete_NoRoleCannotDelete()
    {
        var target = Seeded("UpdateCan");
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Delete(target.Id, _fixture.Organizations["0"].Id));
    }

    private OutputModuleSupply Seeded(string key) =>
        _fixture.SmokeOutputModuleSupplies[$"OutputModuleSupply_SmokeTests_{key}"];

    private Guid OwnerId() =>
        _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id;

    // Module0001 produces, so the seeded rows under Module0000 keep their unique keys.
    private OutputModuleSupply Build(string suffix) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = _fixture.Organizations["0"].Id,
        ModuleId = _fixture.Modules["0001"].Id,
        OutputName = $"{suffix}_{Guid.NewGuid():N}",
        ConsumingModuleId = _fixture.Modules["0003"].Id,
    };

    private OutputModuleSupplySecuredRepository Repo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        return new OutputModuleSupplySecuredRepository(
            new OutputModuleSupplyRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new OutputModuleSupplyRepositorySettings())),
            pp);
    }
}
