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
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.RoleResolution;

/// <summary>
/// Verifies the agent-side ReadQuery overrides on the four <c>*MissionSecuredRepository</c>
/// classes. Each mission scope adds an "agent-keyed" visibility path: a user with
/// <c>AgentRole.Reader</c> on an Agent must see missions where <c>AgentId</c> equals that Agent,
/// regardless of the mission's scope row.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class MissionCrossScope_RoleResolutionTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public MissionCrossScope_RoleResolutionTests(Fixture fixture)
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

    [Fact]
    public async Task OrganizationMission_AgentRoles_BespokeWiring_IsConnected()
    {
        // OrganizationMissions["0"].AgentId == Agents["0"].Id; AgentRole.Reader on Agent0 should
        // see the mission via the bespoke ReadQuery override.
        var principal = _fixture.ScopeReaderUsers["Agent0.Reader"];
        var visible = await ListOrganizationMissions(principal.Id);
        Assert.NotEmpty(visible);
    }

    [Fact]
    public async Task StackMission_AgentRoles_BespokeWiring_IsConnected()
    {
        var principal = _fixture.ScopeReaderUsers["Agent0.Reader"];
        var visible = await ListStackMissions(principal.Id);
        Assert.NotEmpty(visible);
    }

    [Fact]
    public async Task NamespaceMission_AgentRoles_BespokeWiring_IsConnected()
    {
        var principal = _fixture.ScopeReaderUsers["Agent0.Reader"];
        var visible = await ListNamespaceMissions(principal.Id);
        Assert.NotEmpty(visible);
    }

    [Fact]
    public async Task ModuleMission_AgentRoles_BespokeWiring_IsConnected()
    {
        var principal = _fixture.ScopeReaderUsers["Agent0.Reader"];
        var visible = await ListModuleMissions(principal.Id);
        Assert.NotEmpty(visible);
    }

    private async Task<List<OrganizationMission>> ListOrganizationMissions(Guid principalId)
    {
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, orgId);
        var repo = new OrganizationMissionSecuredRepository(
            new OrganizationMissionRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new OrganizationMissionRepositorySettings())),
            pp);
        return await repo.List(orgId);
    }

    private async Task<List<StackMission>> ListStackMissions(Guid principalId)
    {
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, orgId);
        var repo = new StackMissionSecuredRepository(
            new StackMissionRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new StackMissionRepositorySettings())),
            pp);
        return await repo.List(orgId);
    }

    private async Task<List<NamespaceMission>> ListNamespaceMissions(Guid principalId)
    {
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, orgId);
        var repo = new NamespaceMissionSecuredRepository(
            new NamespaceMissionRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new NamespaceMissionRepositorySettings())),
            pp);
        return await repo.List(orgId);
    }

    private async Task<List<ModuleMission>> ListModuleMissions(Guid principalId)
    {
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, orgId);
        var repo = new ModuleMissionSecuredRepository(
            new ModuleMissionRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new ModuleMissionRepositorySettings())),
            pp);
        return await repo.List(orgId);
    }
}
