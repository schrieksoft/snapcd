// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Secured.Crud;
using SnapCd.Server.Core.Services.Secured.Crud.Jobs;
using SnapCd.Server.Core.Services.Secured.Crud.Transfers;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Permissions.ServiceGuards;

/// <summary>
/// A service that projects straight off the DbContext rather than through its secured repository
/// has to apply the permission itself. These reads were reachable by any member of the
/// organization, including over the MCP surface, so each one is covered by a role that may read
/// and a role that may not.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ServiceReadGuardTests
{
    private readonly Fixture _fixture;
    private readonly Guid _moduleId;
    private readonly Guid _organizationId;

    public ServiceReadGuardTests(Fixture fixture)
    {
        _fixture = fixture;
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
    }

    private Guid Reader => _fixture.OrganizationPrincipals["0"][OrganizationRole.Reader].DirectUser.Id;
    private Guid Owner => _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id;

    /// <summary>A user with no role assignment anywhere, which is what an unauthorized caller is.</summary>
    private static readonly Guid Outsider = Guid.NewGuid();

    [Fact]
    public async Task GetSource_Refuses_A_Principal_Without_Read()
    {
        using var service = ModuleService(Outsider);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.GetSource(_moduleId, _organizationId));
    }

    [Fact]
    public async Task GetSource_Allows_A_Reader()
    {
        using var service = ModuleService(Reader);
        var source = await service.GetSource(_moduleId, _organizationId);
        Assert.Equal(_moduleId, source.Id);
    }

    [Fact]
    public async Task GetState_Refuses_A_Principal_Without_Read()
    {
        using var service = ModuleService(Outsider);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.GetState(_moduleId, _organizationId));
    }

    [Fact]
    public async Task GetState_Allows_A_Reader()
    {
        using var service = ModuleService(Reader);
        var state = await service.GetState(_moduleId, _organizationId);
        Assert.Equal(_moduleId, state.Id);
    }

    [Fact]
    public async Task GetMissionHistory_Refuses_A_Principal_Without_Read()
    {
        using var service = ModuleService(Outsider);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.GetMissionHistory(_moduleId, _organizationId));
    }

    [Fact]
    public async Task GetMissionHistory_Allows_A_Reader()
    {
        using var service = ModuleService(Reader);
        var history = await service.GetMissionHistory(_moduleId, _organizationId);
        Assert.NotNull(history);
    }

    [Fact]
    public async Task GetBlockedReason_Refuses_A_Principal_Without_Read()
    {
        using var service = StateMigrationService(Outsider);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.GetBlockedReason(_moduleId, _organizationId));
    }

    [Fact]
    public async Task GetBlockedReason_Allows_A_Reader()
    {
        using var service = StateMigrationService(Reader);
        var reason = await service.GetBlockedReason(_moduleId, _organizationId);

        // The module is not paused in the shared fixture, so there is a reason rather than null.
        Assert.NotNull(reason);
    }

    [Fact]
    public async Task FindOpenForModule_Refuses_A_Principal_Without_Read()
    {
        using var service = TransferService(Outsider);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.FindOpenForModule(_moduleId, _organizationId));
    }

    [Fact]
    public async Task FindOpenForModule_Allows_A_Reader()
    {
        using var service = TransferService(Reader);
        Assert.Null(await service.FindOpenForModule(_moduleId, _organizationId));
    }

    private ModuleSecuredRepository SecuredModules(Guid principalId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            principalId, PrincipalDiscriminator.User, _organizationId);

        return new ModuleSecuredRepository(
            new ModuleRepository(
                _fixture.CreateDbContext(), principalProvider,
                _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider);
    }

    private ModuleService ModuleService(Guid principalId) =>
        new(SecuredModules(principalId), _fixture.CreateDbContext(), null!, null!);

    private StateMigrationService StateMigrationService(Guid principalId) =>
        new(Factory().CreateDbContext(), SecuredModules(principalId));

    private TransferService TransferService(Guid principalId) =>
        new(Factory().CreateDbContext(), SecuredModules(principalId), null!, null!, null!);

    private IDbContextFactory<SnapCdDbContext> Factory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
    }
}
