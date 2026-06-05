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
/// Tier A smoke — Namespace covers GenericStackChildSecuredRepository plus its own bespoke overrides.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class Namespace_SmokeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public Namespace_SmokeTests(Fixture fixture) { _fixture = fixture; }

    public Task InitializeAsync() { _dbContext = _fixture.CreateDbContext(); return Task.CompletedTask; }
    public Task DisposeAsync() { _dbContext?.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_OwnerCanRead()
    {
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var ns = await repo.Get(_fixture.Namespaces["000"].Id, _fixture.Organizations["0"].Id);
        Assert.NotNull(ns);
    }

    [Fact]
    public async Task Get_NoRoleCannotRead()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Get(_fixture.Namespaces["000"].Id, _fixture.Organizations["0"].Id));
    }

    [Fact]
    public async Task List_OwnerSeesNamespace000()
    {
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var items = await repo.List(_fixture.Organizations["0"].Id);
        Assert.Contains(items, n => n.Id == _fixture.Namespaces["000"].Id);
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
        var ns = new Namespace
        {
            Id = Guid.NewGuid(),
            Name = $"{nameof(Create_OwnerCanCreate)}_Namespace_{Guid.NewGuid():N}",
            StackId = _fixture.Stacks["00"].Id,
            OrganizationId = _fixture.Organizations["0"].Id,
        };
        await repo.Create(ns);
        Assert.NotEqual(Guid.Empty, ns.Id);
    }

    [Fact]
    public async Task Create_NoRoleCannotCreate()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        var ns = new Namespace
        {
            Id = Guid.NewGuid(),
            Name = $"{nameof(Create_NoRoleCannotCreate)}_Namespace_{Guid.NewGuid():N}",
            StackId = _fixture.Stacks["00"].Id,
            OrganizationId = _fixture.Organizations["0"].Id,
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Create(ns));
    }

    [Fact]
    public async Task Update_OwnerCanUpdate()
    {
        var target = _fixture.SmokeNamespaces["Namespace_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        target.Name = $"MutatedByTest_{Guid.NewGuid():N}";
        await repo.Update(target);
    }

    [Fact]
    public async Task Update_NoRoleCannotUpdate()
    {
        var target = _fixture.Namespaces["000"];
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Update(target));
    }

    [Fact]
    public async Task Delete_OwnerCanDelete()
    {
        var target = _fixture.SmokeNamespaces["Namespace_SmokeTests_DeleteCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        await repo.Delete(target.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Delete_NoRoleCannotDelete()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Delete(_fixture.Namespaces["000"].Id, _fixture.Organizations["0"].Id));
    }

    private NamespaceSecuredRepository Repo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        return new NamespaceSecuredRepository(
            new NamespaceRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new NamespaceRepositorySettings())),
            pp);
    }
}
