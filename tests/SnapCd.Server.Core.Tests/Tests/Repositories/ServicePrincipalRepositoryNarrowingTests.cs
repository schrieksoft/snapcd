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
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Repositories;

/// <summary>
/// The ServicePrincipal entity backs every OpenIddict application, so the table also holds clients
/// that are not service principals. The repository narrows its reads to real service principals,
/// and that narrowing has to survive a caller supplying a query or a modifier of its own.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ServicePrincipalRepositoryNarrowingTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _orgId;
    private Guid _realId;
    private Guid _otherClientId;

    public ServicePrincipalRepositoryNarrowingTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _orgId = _fixture.Organizations["0"].Id;
        _realId = _fixture.ServicePrincipals["0"].Id;
        _otherClientId = _fixture.NonServicePrincipalClients["0"].Id;
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListLeavesOutClientsThatAreNotServicePrincipals()
    {
        var listed = await Repo().List(_orgId);

        Assert.Contains(listed, sp => sp.Id == _realId);
        Assert.DoesNotContain(listed, sp => sp.Id == _otherClientId);
    }

    /// <summary>
    /// The secured repository always hands its own queryable down, so a narrowing that only
    /// applied when the caller passed nothing would never apply on the path the UI uses.
    /// </summary>
    [Fact]
    public async Task ListStillNarrowsWhenTheCallerSuppliesItsOwnQuery()
    {
        var everything = _dbContext.ServicePrincipals.AsQueryable();

        var listed = await Repo().List(_orgId, everything);

        Assert.Contains(listed, sp => sp.Id == _realId);
        Assert.DoesNotContain(listed, sp => sp.Id == _otherClientId);
    }

    [Fact]
    public async Task ListStillNarrowsWhenTheCallerSuppliesAModifier()
    {
        var listed = await Repo().List(_orgId,
            queryModifier: q => q.Where(sp => sp.DisplayName!.StartsWith("org0-")));

        Assert.Contains(listed, sp => sp.Id == _realId);
        Assert.DoesNotContain(listed, sp => sp.Id == _otherClientId);
    }

    [Fact]
    public async Task ListStillNarrowsWhenTheCallerSuppliesBoth()
    {
        var everything = _dbContext.ServicePrincipals.AsQueryable();

        var listed = await Repo().List(_orgId, everything,
            q => q.Where(sp => sp.DisplayName!.StartsWith("org0-")));

        Assert.Contains(listed, sp => sp.Id == _realId);
        Assert.DoesNotContain(listed, sp => sp.Id == _otherClientId);
    }

    [Fact]
    public async Task CountLeavesOutClientsThatAreNotServicePrincipals()
    {
        var all = await _dbContext.ServicePrincipals
            .CountAsync(sp => sp.OrganizationId == _orgId);
        var counted = await Repo().Count(_orgId);

        Assert.True(counted < all);
        Assert.Equal(all - 1, counted);
    }

    [Fact]
    public async Task ProjectingListNarrowsToo()
    {
        var ids = await Repo().List(_orgId, q => q.Select(sp => sp.Id));

        Assert.Contains(_realId, ids);
        Assert.DoesNotContain(_otherClientId, ids);
    }

    [Fact]
    public async Task GetFindsAServicePrincipal()
    {
        var found = await Repo().Get(_realId, _orgId);

        Assert.Equal(_realId, found.Id);
    }

    [Fact]
    public async Task GetRefusesToReachAClientThatIsNotAServicePrincipal()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => Repo().Get(_otherClientId, _orgId));
    }

    private ServicePrincipalRepository Repo()
    {
        var pp = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            PrincipalDiscriminator.User,
            _orgId);

        return new ServicePrincipalRepository(_dbContext, pp, _fixture.CreateMockBus(),
            Options.Create(new ServicePrincipalRepositorySettings()));
    }
}
