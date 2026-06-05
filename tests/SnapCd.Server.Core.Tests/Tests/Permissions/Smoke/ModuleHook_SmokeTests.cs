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
/// Tier A smoke — ModuleHook is the representative entity for GenericModuleChildSecuredRepository.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ModuleHook_SmokeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public ModuleHook_SmokeTests(Fixture fixture) { _fixture = fixture; }

    public Task InitializeAsync() { _dbContext = _fixture.CreateDbContext(); return Task.CompletedTask; }
    public Task DisposeAsync() { _dbContext?.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_OwnerCanRead()
    {
        var target = _fixture.SmokeModuleHooks["ModuleHook_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var hook = await repo.Get(target.Id, _fixture.Organizations["0"].Id);
        Assert.NotNull(hook);
    }

    [Fact]
    public async Task Get_NoRoleCannotRead()
    {
        var target = _fixture.SmokeModuleHooks["ModuleHook_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Get(target.Id, _fixture.Organizations["0"].Id));
    }

    [Fact]
    public async Task List_OwnerSeesHook()
    {
        var target = _fixture.SmokeModuleHooks["ModuleHook_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        var items = await repo.List(_fixture.Organizations["0"].Id);
        Assert.Contains(items, h => h.Id == target.Id);
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
        var hook = BuildHook("create-allowed");
        await repo.Create(hook);
        Assert.NotEqual(Guid.Empty, hook.Id);
    }

    [Fact]
    public async Task Create_NoRoleCannotCreate()
    {
        var repo = Repo(_fixture.NoPermissionUser.Id);
        var hook = BuildHook("create-denied");
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Create(hook));
    }

    [Fact]
    public async Task Update_OwnerCanUpdate()
    {
        var target = _fixture.SmokeModuleHooks["ModuleHook_SmokeTests_UpdateCan"];
        target.Script = $"echo 'mutated-{Guid.NewGuid():N}'";
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        await repo.Update(target);
    }

    [Fact]
    public async Task Update_NoRoleCannotUpdate()
    {
        var target = _fixture.SmokeModuleHooks["ModuleHook_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repo.Update(target));
    }

    [Fact]
    public async Task Delete_OwnerCanDelete()
    {
        var target = _fixture.SmokeModuleHooks["ModuleHook_SmokeTests_DeleteCan"];
        var repo = Repo(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        await repo.Delete(target.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Delete_NoRoleCannotDelete()
    {
        var target = _fixture.SmokeModuleHooks["ModuleHook_SmokeTests_UpdateCan"];
        var repo = Repo(_fixture.NoPermissionUser.Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repo.Delete(target.Id, _fixture.Organizations["0"].Id));
    }

    private ModuleHook BuildHook(string suffix) => new()
    {
        Id = Guid.NewGuid(),
        // Module0001 (not 0000) — Module0000 already has the seeded UpdateCan + DeleteCan
        // hooks at (Apply, Before/After), and there's a unique index on (ModuleId, Task, Phase).
        OrganizationId = _fixture.Organizations["0"].Id,
        ModuleId = _fixture.Modules["0001"].Id,
        Task = HookTask.Plan,
        Phase = HookPhase.Before,
        Script = $"echo '{suffix}'",
    };

    private ModuleHookSecuredRepository Repo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        return new ModuleHookSecuredRepository(
            new ModuleHookRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new ModuleHookRepositorySettings())),
            pp);
    }
}
