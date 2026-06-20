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
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.AgentSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.AgentSupplies;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.RoleResolution;

/// <summary>
/// Verifies that <c>GenericAgentChildSecuredRepository</c>'s AgentRoles family is wired into
/// the ReadQuery: a user with <c>AgentRole.Reader</c> on an Agent must see at least one of that
/// Agent's child assignments (<c>AgentModuleSupply</c> as the representative entity).
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class AgentChain_RoleResolutionTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public AgentChain_RoleResolutionTests(Fixture fixture)
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
    public async Task GenericAgentChild_AgentRoles_AreWired()
    {
        var principal = _fixture.ScopeReaderUsers["Agent0.Reader"];
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principal.Id, PrincipalDiscriminator.User, orgId);
        var repo = new AgentModuleSupplySecuredRepository(
            new AgentModuleSupplyRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new AgentModuleSupplyRepositorySettings())),
            pp);
        var visible = await repo.List(orgId);
        Assert.NotEmpty(visible);
    }
}
