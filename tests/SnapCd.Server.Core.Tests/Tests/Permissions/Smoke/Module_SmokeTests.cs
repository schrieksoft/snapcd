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
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.Smoke;

/// <summary>
/// Tier A smoke — Module covers GenericNamespaceChildSecuredRepository plus its own bespoke overrides.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class Module_SmokeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public Module_SmokeTests(Fixture fixture) { _fixture = fixture; }

    public Task InitializeAsync() { _dbContext = _fixture.CreateDbContext(); return Task.CompletedTask; }
    public Task DisposeAsync() { _dbContext?.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_OwnerCanRead()
    {
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var module = await repo.Get(_fixture.Modules["0000"].Id, _fixture.Organizations["0"].Id);
        Assert.NotNull(module);
    }

    [Fact]
    public async Task Get_NoRoleCannotRead()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Get(_fixture.Modules["0000"].Id, _fixture.Organizations["0"].Id));
    }

    [Fact]
    public async Task List_OwnerSeesModule0000()
    {
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var items = await repo.List(_fixture.Organizations["0"].Id);
        Assert.Contains(items, m => m.Id == _fixture.Modules["0000"].Id);
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
        var module = BuildModule($"{nameof(Create_OwnerCanCreate)}_Module_{Guid.NewGuid():N}");
        await repo.Create(module);
        Assert.NotEqual(Guid.Empty, module.Id);
    }

    [Fact]
    public async Task Create_NoRoleCannotCreate()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        var module = BuildModule($"{nameof(Create_NoRoleCannotCreate)}_Module_{Guid.NewGuid():N}");
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Create(module));
    }

    [Fact]
    public async Task Update_OwnerCanUpdate()
    {
        var target = _fixture.SmokeModules["Module_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        target.Name = $"MutatedByTest_{Guid.NewGuid():N}";
        await repo.Update(target);
    }

    [Fact]
    public async Task Update_NoRoleCannotUpdate()
    {
        var target = _fixture.Modules["0000"];
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Update(target));
    }

    [Fact]
    public async Task Delete_OwnerCanDelete()
    {
        var target = _fixture.SmokeModules["Module_SmokeTests_DeleteCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        await repo.Delete(target.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Delete_NoRoleCannotDelete()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Delete(_fixture.Modules["0000"].Id, _fixture.Organizations["0"].Id));
    }

    private Module BuildModule(string name)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = name,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            OrganizationId = _fixture.Organizations["0"].Id,
            SourceUrl = $"https://github.com/test/{name.ToLower()}",
            SourceRevision = "main",
            SourceSubdirectory = "terraform",
            CreatedDateTime = DateTime.UtcNow,
        };
        module.ModuleSaga = new ModuleSaga
        {
            CorrelationId = module.Id,
            OrganizationId = module.OrganizationId,
            RowVersion = Array.Empty<byte>(),
            CurrentState = nameof(ModuleStateMachine.Gatekeeping),
            DesiredStateHeadline = DesiredStateHeadline.Applied,
            QueuedDesiredStateHeadline = null,
        };
        module.ModuleModifiedSaga = new ModuleModifiedSaga
        {
            CorrelationId = module.Id,
            OrganizationId = module.OrganizationId,
            RowVersion = Array.Empty<byte>(),
            CurrentState = nameof(ModuleModifiedStateMachine.Idle),
            LastUpdated = null,
            TimeoutTokenId = null,
        };
        return module;
    }

    private ModuleSecuredRepository Repo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        return new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new ModuleRepositorySettings())),
            pp);
    }
}
