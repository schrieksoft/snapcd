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
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Repositories;

/// <summary>
/// Paged listing deduplicates before it orders and cuts the page. Deduplicating afterwards leaves
/// the page unordered — the ordering is applied to a set the Distinct then rebuilds — and can only
/// remove duplicates that happen to land within the same page.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class PagedListOrderingTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public PagedListOrderingTests(Fixture fixture) => _fixture = fixture;

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
    public async Task PagesAreOrderedAndDoNotOverlap()
    {
        var orgId = _fixture.Organizations["0"].Id;

        var pageOne = await Repo().List(orgId, orderBy: q => q.OrderBy(m => m.Name), pageNumber: 1, pageSize: 3);
        var pageTwo = await Repo().List(orgId, orderBy: q => q.OrderBy(m => m.Name), pageNumber: 2, pageSize: 3);

        Assert.Equal(3, pageOne.Count);
        Assert.Equal(pageOne.Select(m => m.Name).OrderBy(n => n), pageOne.Select(m => m.Name));
        Assert.Equal(pageTwo.Select(m => m.Name).OrderBy(n => n), pageTwo.Select(m => m.Name));

        // Consecutive pages of one ordering must not repeat a row, and page two must sort after
        // page one — both fail if the page is cut before the ordering is settled.
        Assert.Empty(pageOne.Select(m => m.Id).Intersect(pageTwo.Select(m => m.Id)));
        Assert.True(string.CompareOrdinal(pageOne.Last().Name, pageTwo.First().Name) < 0);
    }

    [Fact]
    public async Task RepeatedRequestsForTheSamePageAgree()
    {
        var orgId = _fixture.Organizations["0"].Id;

        var first = await Repo().List(orgId, orderBy: q => q.OrderBy(m => m.Name), pageNumber: 2, pageSize: 3);
        var second = await Repo().List(orgId, orderBy: q => q.OrderBy(m => m.Name), pageNumber: 2, pageSize: 3);

        Assert.Equal(first.Select(m => m.Id), second.Select(m => m.Id));
    }

    [Fact]
    public async Task PagingCoversEveryRowExactlyOnce()
    {
        var orgId = _fixture.Organizations["0"].Id;
        var all = await Repo().List(orgId, orderBy: q => q.OrderBy(m => m.Name));

        var paged = new List<Guid>();
        for (var page = 1; paged.Count < all.Count; page++)
        {
            var rows = await Repo().List(orgId, orderBy: q => q.OrderBy(m => m.Name), pageNumber: page, pageSize: 3);
            if (rows.Count == 0) break;
            paged.AddRange(rows.Select(m => m.Id));
        }

        Assert.Equal(all.Select(m => m.Id), paged);
        Assert.Equal(paged.Distinct().Count(), paged.Count);
    }

    private ModuleRepository Repo()
    {
        var pp = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            PrincipalDiscriminator.User,
            _fixture.Organizations["0"].Id);

        return new ModuleRepository(_dbContext, pp, _fixture.CreateMockBus(),
            Options.Create(new ModuleRepositorySettings()));
    }
}
