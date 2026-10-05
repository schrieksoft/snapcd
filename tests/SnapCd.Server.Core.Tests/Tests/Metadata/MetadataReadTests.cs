// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Contracts.Dto.Stacks;
using SnapCd.Server.Core.Views;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Metadata;

/// <summary>
/// Metadata read resolves names and ids for a principal who cannot read the object itself, and
/// stops at the subtree the role was granted on. Each test also asserts the full read is still
/// refused, since a metadata role that accidentally granted read would otherwise pass.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class MetadataReadTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _organizationId;

    public MetadataReadTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task StackMetadataReader_Resolves_Its_Own_Stack()
    {
        var ids = await StackMetadataIds("Stack00.MetadataReader");
        Assert.Contains(_fixture.Stacks["00"].Id, ids);
    }

    [Fact]
    public async Task StackMetadataReader_Does_Not_Resolve_A_Sibling_Stack()
    {
        var ids = await StackMetadataIds("Stack00.MetadataReader");
        Assert.DoesNotContain(_fixture.Stacks["01"].Id, ids);
    }

    [Fact]
    public async Task StackMetadataReader_Cannot_Read_The_Stack_Itself()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.MetadataReader"];
        Assert.False(StackRepo(user.Id).CanRead(_fixture.Stacks["00"].Id, _organizationId));
    }

    [Fact]
    public async Task StackMetadataReader_Reaches_Namespaces_And_Modules_Below_It()
    {
        var namespaceIds = await NamespaceMetadataIds("Stack00.MetadataReader");
        var moduleIds = await ModuleMetadataIds("Stack00.MetadataReader");

        Assert.Contains(_fixture.Namespaces["000"].Id, namespaceIds);
        Assert.Contains(_fixture.Modules["0000"].Id, moduleIds);
    }

    [Fact]
    public async Task NamespaceMetadataReader_Does_Not_Reach_A_Sibling_Namespace()
    {
        var ids = await NamespaceMetadataIds("Namespace000.MetadataReader");

        Assert.Contains(_fixture.Namespaces["000"].Id, ids);
        Assert.DoesNotContain(_fixture.Namespaces["001"].Id, ids);
    }

    [Fact]
    public async Task ModuleMetadataReader_Does_Not_Reach_A_Sibling_Module()
    {
        var ids = await ModuleMetadataIds("Module0000.MetadataReader");

        Assert.Contains(_fixture.Modules["0000"].Id, ids);
        Assert.DoesNotContain(_fixture.Modules["0001"].Id, ids);
    }

    // A Module role reverse-inherits onto its own ancestors, and must not spread sideways from there.
    [Fact]
    public async Task ModuleMetadataReader_Reaches_Its_Own_Ancestors_Only()
    {
        var namespaceIds = await NamespaceMetadataIds("Module0000.MetadataReader");

        Assert.Contains(_fixture.Namespaces["000"].Id, namespaceIds);
        Assert.DoesNotContain(_fixture.Namespaces["001"].Id, namespaceIds);
    }

    [Fact]
    public async Task OrganizationStackMetadataReader_Resolves_Every_Stack()
    {
        var ids = await StackMetadataIds("Org.StackMetadataReader");

        Assert.Contains(_fixture.Stacks["00"].Id, ids);
        Assert.Contains(_fixture.Stacks["01"].Id, ids);
    }

    [Fact]
    public async Task OrganizationStackMetadataReader_Cannot_Read_A_Stack()
    {
        var user = _fixture.ScopeReaderUsers["Org.StackMetadataReader"];
        Assert.False(StackRepo(user.Id).CanRead(_fixture.Stacks["00"].Id, _organizationId));
    }

    // Read implies metadata read, so an ordinary Reader must still resolve metadata.
    [Fact]
    public async Task A_Full_Reader_Also_Resolves_Metadata()
    {
        var ids = await StackMetadataIds("Stack00.Reader");
        Assert.Contains(_fixture.Stacks["00"].Id, ids);
    }

    [Fact]
    public async Task A_Principal_With_No_Role_Resolves_Nothing()
    {
        var sp = _fixture.NoPermissionServicePrincipal;
        var pp = _fixture.CreatePrincipalProvider(sp.Id, PrincipalDiscriminator.ServicePrincipal, _organizationId);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new StackRepositorySettings())),
            pp);

        Assert.Empty(await repo.ReadMetadataQuery(_organizationId).Select(x => x.Id).ToListAsync());
    }

    [Fact]
    public async Task GetMetadata_Resolves_For_A_Metadata_Reader()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.MetadataReader"];
        var result = await StackRepo(user.Id).GetMetadataByName(_fixture.Stacks["00"].Name, _organizationId);

        Assert.Equal(_fixture.Stacks["00"].Id, result.Id);
        Assert.Equal(_fixture.Stacks["00"].Name, result.Name);
    }

    [Fact]
    public async Task GetMetadata_Refuses_A_Sibling_Stack()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.MetadataReader"];

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => StackRepo(user.Id).GetMetadataByName(_fixture.Stacks["01"].Name, _organizationId));
    }

    // The projection is the point: what leaves the API cannot carry the full record.
    // OrganizationId is on the view for the permission predicate and must not reach the DTO.
    [Fact]
    public async Task The_Metadata_Dto_Exposes_Only_Name_And_Id()
    {
        var names = typeof(StackMetadataReadDto).GetProperties().Select(p => p.Name).OrderBy(n => n).ToList();

        Assert.Equal(new[] { "Id", "Name" }, names);
    }

    // The service layer returns the metadata DTO, and the ordinary read path still refuses.
    [Fact]
    public async Task Service_Returns_The_Metadata_Dto_For_A_Metadata_Reader()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.MetadataReader"];
        using var service = new StackService(StackRepo(user.Id));

        var dto = await service.GetMetadataByName(_fixture.Stacks["00"].Name, _organizationId);

        Assert.Equal(_fixture.Stacks["00"].Id, dto.Id);
        Assert.Equal(_fixture.Stacks["00"].Name, dto.Name);
    }

    [Fact]
    public async Task Service_Still_Refuses_The_Ordinary_Read_For_A_Metadata_Reader()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.MetadataReader"];
        using var service = new StackService(StackRepo(user.Id));

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.GetByName(_fixture.Stacks["00"].Name, _organizationId));
    }

    [Fact]
    public async Task Service_ListMetadata_Excludes_A_Sibling_Stack()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.MetadataReader"];
        using var service = new StackService(StackRepo(user.Id));

        var ids = (await service.ListMetadata(_organizationId)).Select(x => x.Id).ToList();

        Assert.Contains(_fixture.Stacks["00"].Id, ids);
        Assert.DoesNotContain(_fixture.Stacks["01"].Id, ids);
    }

    // The picker path: search by name, scoped to what the principal may discover.
    [Fact]
    public async Task ListMetadata_Filters_By_Search_Term()
    {
        var user = _fixture.ScopeReaderUsers["Org.StackMetadataReader"];
        var all = await StackRepo(user.Id).ListMetadata(_organizationId);
        var filtered = await StackRepo(user.Id).ListMetadata(_organizationId,
            q => q.Where(x => x.Name == _fixture.Stacks["00"].Name));

        Assert.Contains(_fixture.Stacks["00"].Id, filtered.Select(x => x.Id));
        Assert.True(filtered.Count < all.Count);
    }

    [Fact]
    public async Task ListMetadata_Respects_Paging()
    {
        var user = _fixture.ScopeReaderUsers["Org.StackMetadataReader"];
        var firstPage = await StackRepo(user.Id).ListMetadata(_organizationId,
            orderBy: q => q.OrderBy(x => x.Name), pageNumber: 1, pageSize: 1);
        var secondPage = await StackRepo(user.Id).ListMetadata(_organizationId,
            orderBy: q => q.OrderBy(x => x.Name), pageNumber: 2, pageSize: 1);

        Assert.Single(firstPage);
        Assert.Single(secondPage);
        Assert.NotEqual(firstPage[0].Id, secondPage[0].Id);
    }

    [Fact]
    public async Task ListMetadata_Excludes_A_Stack_The_Principal_Cannot_Discover()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.MetadataReader"];
        var ids = (await StackRepo(user.Id).ListMetadata(_organizationId)).Select(x => x.Id).ToList();

        Assert.Contains(_fixture.Stacks["00"].Id, ids);
        Assert.DoesNotContain(_fixture.Stacks["01"].Id, ids);
    }

    [Fact]
    public async Task GetMetadata_By_Id_Refuses_A_Stack_The_Principal_Cannot_Discover()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.MetadataReader"];

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => StackRepo(user.Id).GetMetadata(_fixture.Stacks["01"].Id, _organizationId));
    }

    // Metadata read must never imply full read. Without this, a metadata map that accidentally
    // carried the read roles would satisfy every other test in this file.
    [Theory]
    [InlineData("Stack00.MetadataReader")]
    [InlineData("Namespace000.MetadataReader")]
    [InlineData("Module0000.MetadataReader")]
    [InlineData("Org.StackMetadataReader")]
    public void A_Metadata_Role_Does_Not_Grant_Read_On_Any_Scope(string userKey)
    {
        var user = _fixture.ScopeReaderUsers[userKey];
        var pp = _fixture.CreatePrincipalProvider(user.Id, PrincipalDiscriminator.User, _organizationId);

        using var stackRepo = new StackSecuredRepository(
            new StackRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new StackRepositorySettings())), pp);
        using var namespaceRepo = new NamespaceSecuredRepository(
            new NamespaceRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new NamespaceRepositorySettings())), pp);
        using var moduleRepo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new ModuleRepositorySettings())), pp);

        Assert.False(stackRepo.CanRead(_fixture.Stacks["00"].Id, _organizationId));
        Assert.False(namespaceRepo.CanRead(_fixture.Namespaces["000"].Id, _organizationId));
        Assert.False(moduleRepo.CanRead(_fixture.Modules["0000"].Id, _organizationId));
    }

    // Read implies metadata read, so the ordinary Reader must satisfy both.
    [Fact]
    public void A_Read_Role_Grants_Both_Read_And_Metadata_Read()
    {
        var user = _fixture.ScopeReaderUsers["Stack00.Reader"];

        Assert.True(StackRepo(user.Id).CanRead(_fixture.Stacks["00"].Id, _organizationId));
        Assert.True(StackRepo(user.Id).CanReadMetadata(_fixture.Stacks["00"].Id, _organizationId));
    }

    private StackSecuredRepository StackRepo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new StackSecuredRepository(
            new StackRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new StackRepositorySettings())),
            pp);
    }

    private async Task<List<Guid>> StackMetadataIds(string userKey)
    {
        var user = _fixture.ScopeReaderUsers[userKey];
        return await StackRepo(user.Id).ReadMetadataQuery(_organizationId).Select(x => x.Id).Distinct().ToListAsync();
    }

    private async Task<List<Guid>> NamespaceMetadataIds(string userKey)
    {
        var user = _fixture.ScopeReaderUsers[userKey];
        var pp = _fixture.CreatePrincipalProvider(user.Id, PrincipalDiscriminator.User, _organizationId);
        var repo = new NamespaceSecuredRepository(
            new NamespaceRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new NamespaceRepositorySettings())),
            pp);
        return await repo.ReadMetadataQuery(_organizationId).Select(x => x.Id).Distinct().ToListAsync();
    }

    private async Task<List<Guid>> ModuleMetadataIds(string userKey)
    {
        var user = _fixture.ScopeReaderUsers[userKey];
        var pp = _fixture.CreatePrincipalProvider(user.Id, PrincipalDiscriminator.User, _organizationId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, pp, _fixture.CreateMockBus(), Options.Create(new ModuleRepositorySettings())),
            pp);
        return await repo.ReadMetadataQuery(_organizationId).Select(x => x.Id).Distinct().ToListAsync();
    }
}
