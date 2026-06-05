// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.PrincipalSource;

/// <summary>
/// Tier C — principal-source dispatch. Proves the role-resolution machinery treats all four
/// principal sources (direct User, direct ServicePrincipal, GroupMember, NestedGroupMember)
/// identically when they hold the same role on the same scope. Runs against Stack as the
/// simplest representative entity; the dispatch is shared infrastructure, so passing once
/// here covers every entity.
///
/// All four positive principals hold OrganizationRole.Owner on Org0 via different mechanisms.
/// All four CanGet(Stack0) calls must succeed; the no-permission control must fail.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class PrincipalDispatch_Tests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private StackTestActions _stackActions = null!;

    public PrincipalDispatch_Tests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _stackActions = new StackTestActions(_fixture, _dbContext);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task User_DirectRoleAssignment_CanReadStack0()
    {
        var principal = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        await _stackActions.CanGet(principal.Id, PrincipalDiscriminator.User, _fixture.Stacks["00"].Id);
    }

    [Fact]
    public async Task ServicePrincipal_DirectRoleAssignment_CanReadStack0()
    {
        var principal = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal;
        await _stackActions.CanGet(principal.Id, PrincipalDiscriminator.ServicePrincipal, _fixture.Stacks["00"].Id);
    }

    [Fact]
    public async Task User_ViaGroupMembership_CanReadStack0()
    {
        var principal = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].GroupUser;
        await _stackActions.CanGet(principal.Id, PrincipalDiscriminator.User, _fixture.Stacks["00"].Id);
    }

    [Fact]
    public async Task User_ViaNestedGroupMembership_CanReadStack0()
    {
        var principal = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].NestedGroupUser;
        await _stackActions.CanGet(principal.Id, PrincipalDiscriminator.User, _fixture.Stacks["00"].Id);
    }

    [Fact]
    public async Task User_NoRoleAnywhere_CannotReadStack0()
    {
        await _stackActions.CannotGet(
            _fixture.NoPermissionUser.Id,
            PrincipalDiscriminator.User,
            _fixture.Stacks["00"].Id);
    }
}
