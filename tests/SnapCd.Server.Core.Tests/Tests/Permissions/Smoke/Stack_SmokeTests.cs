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
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.Smoke;

/// <summary>
/// Tier A smoke — Stack as the representative entity for GenericOrganizationChildSecuredRepository.
/// One positive + one negative per action: Get / List / Create / Update / Delete.
/// Positive uses OrganizationOwner; negative uses NoPermissionUser.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class Stack_SmokeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public Stack_SmokeTests(Fixture fixture) { _fixture = fixture; }

    public Task InitializeAsync() { _dbContext = _fixture.CreateDbContext(); return Task.CompletedTask; }
    public Task DisposeAsync() { _dbContext?.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_OwnerCanRead()
    {
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var stack = await repo.Get(_fixture.Stacks["00"].Id, _fixture.Organizations["0"].Id);
        Assert.NotNull(stack);
    }

    [Fact]
    public async Task Get_NoRoleCannotRead()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Get(_fixture.Stacks["00"].Id, _fixture.Organizations["0"].Id));
    }

    [Fact]
    public async Task List_OwnerSeesStack0()
    {
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var stacks = await repo.List(_fixture.Organizations["0"].Id);
        Assert.Contains(stacks, s => s.Id == _fixture.Stacks["00"].Id);
    }

    [Fact]
    public async Task List_NoRoleSeesNothing()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        var stacks = await repo.List(_fixture.Organizations["0"].Id);
        Assert.Empty(stacks);
    }

    [Fact]
    public async Task Create_OwnerCanCreate()
    {
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var newStack = new Stack
        {
            Id = Guid.NewGuid(),
            Name = $"{nameof(Create_OwnerCanCreate)}_Stack_{Guid.NewGuid():N}",
            OrganizationId = _fixture.Organizations["0"].Id,
        };
        await repo.Create(newStack);
        Assert.NotEqual(Guid.Empty, newStack.Id);
    }

    [Fact]
    public async Task Create_NoRoleCannotCreate()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        var newStack = new Stack
        {
            Id = Guid.NewGuid(),
            Name = $"{nameof(Create_NoRoleCannotCreate)}_Stack_{Guid.NewGuid():N}",
            OrganizationId = _fixture.Organizations["0"].Id,
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Create(newStack));
    }

    [Fact]
    public async Task Update_OwnerCanUpdate()
    {
        var target = _fixture.SmokeStacks["Stack_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        target.Name = $"MutatedByTest_{Guid.NewGuid():N}";
        await repo.Update(target);
    }

    [Fact]
    public async Task Update_NoRoleCannotUpdate()
    {
        var target = _fixture.Stacks["00"]; // any pre-seeded row not owned by NoPermissionUser
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Update(target));
    }

    [Fact]
    public async Task Delete_OwnerCanDelete()
    {
        var target = _fixture.SmokeStacks["Stack_SmokeTests_DeleteCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        await repo.Delete(target.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Delete_NoRoleCannotDelete()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Delete(_fixture.Stacks["00"].Id, _fixture.Organizations["0"].Id));
    }

    private StackSecuredRepository Repo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        return new StackSecuredRepository(
            new StackRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new StackRepositorySettings())),
            pp);
    }
}
