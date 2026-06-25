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
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerSupplies;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.Smoke;

/// <summary>
/// Tier A smoke — RunnerModuleSupply is the representative entity for GenericRunnerChildSecuredRepository.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class RunnerModuleSupply_SmokeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public RunnerModuleSupply_SmokeTests(Fixture fixture) { _fixture = fixture; }

    public Task InitializeAsync() { _dbContext = _fixture.CreateDbContext(); return Task.CompletedTask; }
    public Task DisposeAsync() { _dbContext?.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_OwnerCanRead()
    {
        var assignment = await GetFirstAssignment(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        Assert.NotNull(assignment);
    }

    [Fact]
    public async Task Get_NoRoleCannotRead()
    {
        var existing = _fixture.RunnerModuleSupplies["0Sibling"];
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Get(existing.Id, _fixture.Organizations["0"].Id));
    }

    [Fact]
    public async Task List_OwnerSeesAssignments()
    {
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var items = await repo.List(_fixture.Organizations["0"].Id);
        Assert.NotEmpty(items);
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
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var assignment = new RunnerModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            ModuleId = _fixture.Modules["0004"].Id,
        };
        await repo.Create(assignment);
        Assert.NotEqual(Guid.Empty, assignment.Id);
    }

    [Fact]
    public async Task Create_NoRoleCannotCreate()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        var assignment = new RunnerModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            ModuleId = _fixture.Modules["0010"].Id,
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Create(assignment));
    }

    [Fact]
    public async Task Update_OwnerCanUpdate()
    {
        var target = _fixture.SmokeRunnerSupplies["RunnerModuleSupply_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        await repo.Update(target);
    }

    [Fact]
    public async Task Update_NoRoleCannotUpdate()
    {
        var target = _fixture.RunnerModuleSupplies["0Sibling"];
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Update(target));
    }

    [Fact]
    public async Task Delete_OwnerCanDelete()
    {
        var target = _fixture.SmokeRunnerSupplies["RunnerModuleSupply_SmokeTests_DeleteCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        await repo.Delete(target.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Delete_NoRoleCannotDelete()
    {
        var target = _fixture.RunnerModuleSupplies["0Sibling"];
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Delete(target.Id, _fixture.Organizations["0"].Id));
    }

    private async Task<RunnerModuleSupply?> GetFirstAssignment(Guid principalId)
    {
        var repo = Repo(principalId);
        var items = await repo.List(_fixture.Organizations["0"].Id);
        return items.FirstOrDefault();
    }

    private RunnerModuleSupplySecuredRepository Repo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        return new RunnerModuleSupplySecuredRepository(
            new RunnerModuleSupplyRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new RunnerModuleSupplyRepositorySettings())),
            pp);
    }
}
