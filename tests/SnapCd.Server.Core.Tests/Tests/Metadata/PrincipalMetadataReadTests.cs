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
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Metadata;

/// <summary>
/// Granting a role at a scope is allowed without an organization-level role, but choosing who to
/// grant it to means seeing the organization's principals. IdentityAccessMetadataReader grants
/// that much and no more: an id and a label for a user, a service principal and a group.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class PrincipalMetadataReadTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _organizationId;
    private Guid _readerId;
    private Guid _noRoleId;

    public PrincipalMetadataReadTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _organizationId = _fixture.Organizations["0"].Id;
        _readerId = _fixture.ScopeReaderUsers["Org.IdentityAccessMetadataReader"].Id;
        _noRoleId = _fixture.ScopeReaderUsers["Org.StackMetadataReader"].Id;
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ReaderResolvesOrganizationUsers()
    {
        var listed = await UserRepo(_readerId).ListMetadata(_organizationId);

        Assert.NotEmpty(listed);
        Assert.All(listed, u => Assert.False(string.IsNullOrWhiteSpace(u.UserName)));
    }

    /// <summary>
    /// A role assignment references the User's id, so that is what the metadata hands back. The
    /// organization membership row has an id of its own which is the key of nothing.
    /// </summary>
    [Fact]
    public async Task UserMetadataIdIsTheUserIdARoleAssignmentReferences()
    {
        var expected = _fixture.ScopeReaderUsers["Stack00.Reader"].Id;

        var listed = await UserRepo(_readerId).ListMetadata(_organizationId);
        var resolved = listed.Single(u => u.Id == expected);

        Assert.Equal(expected, resolved.Id);
        Assert.True(await _dbContext.UserStackRoleAssignments
            .AnyAsync(ra => ra.UserId == resolved.Id && ra.OrganizationId == _organizationId));
    }

    [Fact]
    public async Task UserMetadataResolvesByUsername()
    {
        var expected = _fixture.ScopeReaderUsers["Stack00.Reader"];

        var resolved = await UserRepo(_readerId).GetMetadataByUsername(expected.Email!, _organizationId);

        Assert.Equal(expected.Id, resolved.Id);
        Assert.Equal(expected.Email, resolved.UserName);
    }

    [Fact]
    public async Task ReaderResolvesServicePrincipals()
    {
        var listed = await ServicePrincipalRepo(_readerId).ListMetadata(_organizationId);

        Assert.Contains(listed, sp => sp.Id == _fixture.ServicePrincipals["0"].Id);
    }

    /// <summary>
    /// The client id is stored with the organization as a prefix, and the metadata hands back the
    /// name on its own.
    /// </summary>
    [Fact]
    public async Task ServicePrincipalMetadataLeavesOffTheOrganizationPrefix()
    {
        var listed = await ServicePrincipalRepo(_readerId).ListMetadata(_organizationId);
        var seeded = listed.Single(sp => sp.Id == _fixture.ServicePrincipals["0"].Id);

        Assert.Equal("org0-sp", seeded.ClientId);
    }

    [Fact]
    public async Task ReaderResolvesGroups()
    {
        var listed = await GroupRepo(_readerId).ListMetadata(_organizationId);

        Assert.NotEmpty(listed);
    }

    [Fact]
    public void ReaderCannotReadAPrincipalItself()
    {
        var spId = _fixture.ServicePrincipals["0"].Id;

        Assert.True(ServicePrincipalRepo(_readerId).CanReadMetadata(spId, _organizationId));
        Assert.False(ServicePrincipalRepo(_readerId).CanRead(spId, _organizationId));
    }

    [Fact]
    public async Task AnotherMetadataRoleResolvesNothingHere()
    {
        Assert.Empty(await UserRepo(_noRoleId).ListMetadata(_organizationId));
        Assert.Empty(await ServicePrincipalRepo(_noRoleId).ListMetadata(_organizationId));
        Assert.Empty(await GroupRepo(_noRoleId).ListMetadata(_organizationId));
    }

    /// <summary>
    /// A stack owner holds nothing at organization level, so the only thing that can let them read
    /// principal metadata is the role derived from the stack role they do hold.
    /// </summary>
    [Fact]
    public async Task AStackOwnerResolvesPrincipalsWithoutAnOrganizationRole()
    {
        var stackOwnerId = _fixture.ScopeReaderUsers["Stack00.OwnerForDerivation"].Id;

        Assert.Empty(await _dbContext.UserOrganizationRoleAssignments
            .Where(ra => ra.UserId == stackOwnerId && ra.OrganizationId == _organizationId)
            .ToListAsync());

        Assert.NotEmpty(await UserRepo(stackOwnerId).ListMetadata(_organizationId));
        Assert.NotEmpty(await ServicePrincipalRepo(stackOwnerId).ListMetadata(_organizationId));
    }

    [Fact]
    public async Task TheDerivedRoleIsRecordedSeparatelyFromGrantedOnes()
    {
        var stackOwnerId = _fixture.ScopeReaderUsers["Stack00.OwnerForDerivation"].Id;

        var derived = await _dbContext.DerivedOrganizationRoleAssignments
            .Where(d => d.PrincipalId == stackOwnerId && d.OrganizationId == _organizationId)
            .ToListAsync();

        Assert.Single(derived);
        Assert.Equal(OrganizationRole.IdentityAccessMetadataReader, derived[0].RoleName);
    }

    /// <summary>
    /// Reader is not a role that may grant anything, so it must not pick the derivation up.
    /// </summary>
    [Fact]
    public async Task AReaderResolvesNothing()
    {
        var readerId = _fixture.ScopeReaderUsers["Stack00.MetadataReader"].Id;

        Assert.Empty(await _dbContext.DerivedOrganizationRoleAssignments
            .Where(d => d.PrincipalId == readerId && d.OrganizationId == _organizationId)
            .ToListAsync());

        Assert.Empty(await UserRepo(readerId).ListMetadata(_organizationId));
    }

    private OrganizationUserSecuredRepository UserRepo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new OrganizationUserSecuredRepository(
            new OrganizationUserRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new OrganizationUserRepositorySettings())),
            pp);
    }

    private ServicePrincipalSecuredRepository ServicePrincipalRepo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new ServicePrincipalSecuredRepository(
            new ServicePrincipalRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new ServicePrincipalRepositorySettings())),
            pp);
    }

    private GroupSecuredRepository GroupRepo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new GroupSecuredRepository(
            new GroupRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new GroupRepositorySettings())),
            pp);
    }
}
