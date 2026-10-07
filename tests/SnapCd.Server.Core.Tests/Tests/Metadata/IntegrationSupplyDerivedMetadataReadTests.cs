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
/// An Integration supplied to a stack, namespace or module can be named by anyone who can read
/// that scope. A role above the supply point reaches it, a role below it does not, and a role at a
/// sibling of the supply point does not.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class IntegrationSupplyDerivedMetadataReadTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _organizationId;

    public IntegrationSupplyDerivedMetadataReadTests(Fixture fixture) => _fixture = fixture;

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
    public async Task AModuleReaderResolvesTheIntegrationSuppliedToThatModule()
    {
        var listed = await Repo("Module0000.Reader").ListMetadata(_organizationId);

        Assert.Contains(listed, i => i.Id == _fixture.Integrations["Module"].Id);
    }

    [Fact]
    public async Task AModuleReaderDoesNotResolveTheIntegrationSuppliedToASiblingModule()
    {
        var listed = await Repo("Module0000.Reader").ListMetadata(_organizationId);

        Assert.DoesNotContain(listed, i => i.Id == _fixture.Integrations["ModuleSibling"].Id);
    }

    /// <summary>
    /// The namespace contains the module, so a namespace role reaches a module supply.
    /// </summary>
    [Fact]
    public async Task ANamespaceReaderResolvesIntegrationsSuppliedToItsNamespaceAndItsModules()
    {
        var listed = await Repo("Namespace000.Reader").ListMetadata(_organizationId);

        Assert.Contains(listed, i => i.Id == _fixture.Integrations["Namespace"].Id);
        Assert.Contains(listed, i => i.Id == _fixture.Integrations["Module"].Id);
        Assert.Contains(listed, i => i.Id == _fixture.Integrations["ModuleSibling"].Id);
    }

    /// <summary>
    /// A module role does not reach a supply made at the namespace above it.
    /// </summary>
    [Fact]
    public async Task AModuleReaderDoesNotResolveTheIntegrationSuppliedToItsNamespace()
    {
        var listed = await Repo("Module0000.Reader").ListMetadata(_organizationId);

        Assert.DoesNotContain(listed, i => i.Id == _fixture.Integrations["Namespace"].Id);
        Assert.DoesNotContain(listed, i => i.Id == _fixture.Integrations["Stack"].Id);
    }

    [Fact]
    public async Task AStackReaderResolvesIntegrationsSuppliedAnywhereBeneathIt()
    {
        var listed = await Repo("Stack00.Reader").ListMetadata(_organizationId);

        Assert.Contains(listed, i => i.Id == _fixture.Integrations["Stack"].Id);
        Assert.Contains(listed, i => i.Id == _fixture.Integrations["Namespace"].Id);
        Assert.Contains(listed, i => i.Id == _fixture.Integrations["Module"].Id);
    }

    /// <summary>
    /// Stack01 is a sibling of the stack everything is supplied under, so its reader resolves none
    /// of it.
    /// </summary>
    [Fact]
    public async Task ASiblingStackReaderResolvesNoneOfIt()
    {
        var listed = await Repo("Stack01.Reader").ListMetadata(_organizationId);

        Assert.DoesNotContain(listed, i => i.Id == _fixture.Integrations["Stack"].Id);
        Assert.DoesNotContain(listed, i => i.Id == _fixture.Integrations["Namespace"].Id);
        Assert.DoesNotContain(listed, i => i.Id == _fixture.Integrations["Module"].Id);
    }

    [Fact]
    public async Task AnIntegrationSuppliedToEveryModuleIsResolvedByAnyModuleRole()
    {
        var listed = await Repo("Module0000.Reader").ListMetadata(_organizationId);

        Assert.Contains(listed, i => i.Id == _fixture.Integrations["AllModules"].Id);
    }

    /// <summary>
    /// Nothing is supplied, so no scope role reaches it.
    /// </summary>
    [Fact]
    public async Task AnIntegrationSuppliedNowhereIsResolvedByNoScopeRole()
    {
        var unsuppliedId = _fixture.Integrations["Unsupplied"].Id;

        foreach (var key in new[] { "Module0000.Reader", "Namespace000.Reader", "Stack00.Reader" })
        {
            var listed = await Repo(key).ListMetadata(_organizationId);
            Assert.DoesNotContain(listed, i => i.Id == unsuppliedId);
        }
    }

    /// <summary>
    /// A metadata-only role on the scope does not chain into metadata read on what is supplied there.
    /// </summary>
    [Fact]
    public async Task AModuleMetadataReaderResolvesNothingSuppliedToThatModule()
    {
        var listed = await Repo("Module0000.MetadataReader").ListMetadata(_organizationId);

        Assert.DoesNotContain(listed, i => i.Id == _fixture.Integrations["Module"].Id);
    }

    [Fact]
    public void TheSupplyCarriesMetadataReadAndNothingMore()
    {
        var repo = Repo("Module0000.Reader");
        var suppliedId = _fixture.Integrations["Module"].Id;

        Assert.True(repo.CanReadMetadata(suppliedId, _organizationId));
        Assert.False(repo.CanRead(suppliedId, _organizationId));
    }

    [Fact]
    public async Task TheDerivedRoleIsRecordedSeparatelyFromGrantedOnes()
    {
        var readerId = _fixture.ScopeReaderUsers["Module0000.Reader"].Id;
        var suppliedId = _fixture.Integrations["Module"].Id;

        var derived = await _dbContext.DerivedIntegrationRoleAssignments
            .Where(d => d.PrincipalId == readerId
                        && d.IntegrationId == suppliedId
                        && d.OrganizationId == _organizationId)
            .ToListAsync();

        Assert.Single(derived);
        Assert.Equal(IntegrationRole.MetadataReader, derived[0].RoleName);

        Assert.Empty(await _dbContext.UserIntegrationRoleAssignments
            .Where(ra => ra.UserId == readerId && ra.IntegrationId == suppliedId)
            .ToListAsync());
    }

    private IntegrationSecuredRepository Repo(string scopeReaderKey)
    {
        var principalId = _fixture.ScopeReaderUsers[scopeReaderKey].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new IntegrationSecuredRepository(
            new IntegrationRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new IntegrationRepositorySettings())),
            pp);
    }
}
