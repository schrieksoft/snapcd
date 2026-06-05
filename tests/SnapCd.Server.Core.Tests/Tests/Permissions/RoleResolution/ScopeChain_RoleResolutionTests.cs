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
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.RoleResolution;

/// <summary>
/// Tier B — per-family wiring verification for the scope generic bases
/// (GenericStackChildSecuredRepository, GenericNamespaceChildSecuredRepository,
/// GenericModuleChildSecuredRepository).
///
/// Each test seeds a principal holding the minimum sufficient role from one declared role family
/// and asserts the corresponding ReadQuery returns a non-empty result. A failing test indicates
/// the family's join is missing or wrong in that generic base's RoleQuery override.
///
/// OrganizationRoles wiring is already covered by Tier A smoke; only the non-Org families are
/// verified here.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ScopeChain_RoleResolutionTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public ScopeChain_RoleResolutionTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    // ---- GenericStackChildSecuredRepository (Namespace as rep) ----

    [Fact]
    public async Task GenericStackChild_StackRoles_AreWired()
    {
        // Namespace's PermissionMap declares StackRoles. If the join is wired, a user with
        // StackRole.Reader on Stack00 should see Namespace000 (a child of Stack00).
        var principal = _fixture.ScopeReaderUsers["Stack00.Reader"];
        var visible = await ListNamespaces(principal.Id);
        Assert.NotEmpty(visible);
    }

    // ---- GenericNamespaceChildSecuredRepository (Module as rep) ----

    [Fact]
    public async Task GenericNamespaceChild_StackRoles_AreWired()
    {
        // Module's PermissionMap declares StackRoles. If the join is wired, a user with
        // StackRole.Reader on Stack00 should see Module0000 (a transitive child of Stack00).
        var principal = _fixture.ScopeReaderUsers["Stack00.Reader"];
        var visible = await ListModules(principal.Id);
        Assert.NotEmpty(visible);
    }

    [Fact]
    public async Task GenericNamespaceChild_NamespaceRoles_AreWired()
    {
        // Module's PermissionMap declares NamespaceRoles. If wired, NamespaceRole.Reader on
        // Namespace000 should see Module0000.
        var principal = _fixture.ScopeReaderUsers["Namespace000.Reader"];
        var visible = await ListModules(principal.Id);
        Assert.NotEmpty(visible);
    }

    // ---- GenericModuleChildSecuredRepository (ModuleHook as rep) ----

    [Fact]
    public async Task GenericModuleChild_StackRoles_AreWired()
    {
        var principal = _fixture.ScopeReaderUsers["Stack00.Reader"];
        var visible = await ListModuleHooks(principal.Id);
        Assert.NotEmpty(visible);
    }

    [Fact]
    public async Task GenericModuleChild_NamespaceRoles_AreWired()
    {
        var principal = _fixture.ScopeReaderUsers["Namespace000.Reader"];
        var visible = await ListModuleHooks(principal.Id);
        Assert.NotEmpty(visible);
    }

    [Fact]
    public async Task GenericModuleChild_ModuleRoles_AreWired()
    {
        var principal = _fixture.ScopeReaderUsers["Module0000.Reader"];
        var visible = await ListModuleHooks(principal.Id);
        Assert.NotEmpty(visible);
    }

    private async Task<List<Entities.Definition.Namespace>> ListNamespaces(Guid principalId)
    {
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, orgId);
        var repo = new NamespaceSecuredRepository(
            new NamespaceRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new NamespaceRepositorySettings())),
            pp);
        return await repo.List(orgId);
    }

    private async Task<List<Entities.Definition.Module>> ListModules(Guid principalId)
    {
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new ModuleRepositorySettings())),
            pp);
        return await repo.List(orgId);
    }

    private async Task<List<Entities.Definition.ModuleHook>> ListModuleHooks(Guid principalId)
    {
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, orgId);
        var repo = new ModuleHookSecuredRepository(
            new ModuleHookRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new ModuleHookRepositorySettings())),
            pp);
        return await repo.List(orgId);
    }
}
