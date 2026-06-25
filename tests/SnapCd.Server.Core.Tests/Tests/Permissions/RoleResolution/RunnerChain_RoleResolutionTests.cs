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
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerSupplies;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.RoleResolution;

/// <summary>
/// Verifies that <c>GenericRunnerChildSecuredRepository</c>'s RunnerRoles family is wired into
/// the ReadQuery: a user with <c>RunnerRole.Reader</c> on a Runner must see at least one of that
/// Runner's child assignments (<c>RunnerModuleSupply</c> as the representative entity).
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class RunnerChain_RoleResolutionTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public RunnerChain_RoleResolutionTests(Fixture fixture)
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
    public async Task GenericRunnerChild_RunnerRoles_AreWired()
    {
        // RunnerPrincipals["0"][RunnerRole.Reader].DirectUser holds RunnerRole.Reader on Runner0;
        // the fixture's CreateRunnerRolePrincipals_Org0 seeds the assignment from Runner0 to Module0000.
        var principal = _fixture.RunnerPrincipals["0"][RunnerRole.Reader].DirectUser;
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principal.Id, PrincipalDiscriminator.User, orgId);
        var repo = new RunnerModuleSupplySecuredRepository(
            new RunnerModuleSupplyRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new RunnerModuleSupplyRepositorySettings())),
            pp);
        var visible = await repo.List(orgId);
        Assert.NotEmpty(visible);
    }
}
