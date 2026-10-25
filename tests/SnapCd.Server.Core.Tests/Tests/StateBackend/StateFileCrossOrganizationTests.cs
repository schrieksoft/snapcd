// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.StateBackend;

/// <summary>
/// The state backend addresses a state file by store id and name, both taken from the request
/// path. A principal of one organization must not reach a store belonging to another, and a
/// principal of the owning organization must still reach its own.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class StateFileCrossOrganizationTests : IAsyncLifetime
{
    private readonly Fixture _fixture;

    private Guid _orgA;
    private Guid _orgB;
    private Guid _storeA;
    private Guid _fileA;
    private Guid _principalA;
    private Guid _principalB;

    public StateFileCrossOrganizationTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _orgA = _fixture.Organizations["0"].Id;
        _orgB = _fixture.Organizations["1"].Id;
        _principalA = _fixture.ServicePrincipals["0"].Id;

        await using var db = _fixture.CreateDbContext();

        // The fixture seeds org0-sp without any role assignment.
        if (!await db.ServicePrincipalOrganizationRoleAssignments
                .AnyAsync(ra => ra.ServicePrincipalId == _principalA && ra.OrganizationId == _orgA))
        {
            db.ServicePrincipalOrganizationRoleAssignments.Add(new ServicePrincipalOrganizationRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = _orgA,
                ServicePrincipalId = _principalA,
                RoleName = OrganizationRole.Contributor
            });
        }

        // Owner in its own organization, the strongest position an outside principal can hold.
        var name = $"org1-outsider-{Guid.NewGuid():N}";
        var spB = new ServicePrincipal
        {
            Id = Guid.NewGuid(),
            DisplayName = name,
            ClientId = $"{_orgB}:{name}",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            Permissions = $"[\"{OpenIddictConstants.Permissions.GrantTypes.ClientCredentials}\"]",
            IsDisabled = false,
            OrganizationId = _orgB
        };
        db.ServicePrincipals.Add(spB);
        db.ServicePrincipalOrganizationRoleAssignments.Add(new ServicePrincipalOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgB,
            ServicePrincipalId = spB.Id,
            RoleName = OrganizationRole.Owner
        });
        _principalB = spB.Id;

        var store = new StateStore
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgA,
            Name = $"cross-org-store-{Guid.NewGuid():N}"
        };
        db.Set<StateStore>().Add(store);
        _storeA = store.Id;

        var file = new StateFile
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgA,
            StateStoreId = store.Id,
            Name = "prod-db"
        };
        db.Set<StateFile>().Add(file);
        _fileA = file.Id;

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FindByName_returns_the_file_to_its_own_organization()
    {
        var found = await Repository(_principalA, _orgA).FindByName("prod-db", _storeA, _orgA);

        Assert.NotNull(found);
        Assert.Equal(_fileA, found!.Id);
    }

    [Fact]
    public async Task FindByName_returns_null_to_another_organization()
    {
        var found = await Repository(_principalB, _orgB).FindByName("prod-db", _storeA, _orgB);

        Assert.Null(found);
    }

    [Fact]
    public async Task FindByName_returns_null_for_a_file_that_does_not_exist()
    {
        var found = await Repository(_principalA, _orgA).FindByName("never-written", _storeA, _orgA);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByName_refuses_another_organization()
    {
        await Assert.ThrowsAnyAsync<Exception>(
            () => Repository(_principalB, _orgB).GetByName("prod-db", _storeA, _orgB));
    }

    [Fact]
    public void Version_reads_are_refused_to_another_organization()
    {
        Assert.Throws<PrincipalNotAuthorizedException>(
            () => Repository(_principalB, _orgB).AssertCanReadVersions(_fileA, _orgB));
    }

    [Fact]
    public void Version_writes_are_refused_to_another_organization()
    {
        Assert.Throws<PrincipalNotAuthorizedException>(
            () => Repository(_principalB, _orgB).AssertCanWriteVersions(_fileA, _orgB));
    }

    [Fact]
    public void Version_reads_are_allowed_to_the_owning_organization()
    {
        Repository(_principalA, _orgA).AssertCanReadVersions(_fileA, _orgA);
    }

    private StateFileSecuredRepository Repository(Guid principalId, Guid organizationId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            principalId, PrincipalDiscriminator.ServicePrincipal, organizationId);

        return new StateFileSecuredRepository(
            new StateFileRepository(
                _fixture.CreateDbContext(),
                principalProvider,
                _fixture.CreateMockBus(),
                Options.Create(new StateFileRepositorySettings())),
            principalProvider);
    }
}
