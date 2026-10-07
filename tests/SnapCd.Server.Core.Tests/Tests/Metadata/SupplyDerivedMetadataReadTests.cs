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
/// Supplying a Runner or Agent to a scope is what makes it usable there, so a role on that scope
/// carries metadata read on the supplied thing. The entitlement is derived by trigger into
/// Derived{Runner,Agent}RoleAssignments, and nothing is granted on the Runner or Agent itself.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class SupplyDerivedMetadataReadTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _organizationId;

    public SupplyDerivedMetadataReadTests(Fixture fixture) => _fixture = fixture;

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
    public async Task AModuleReaderResolvesTheAgentSuppliedToThatModule()
    {
        var readerId = _fixture.ScopeReaderUsers["Module0000.Reader"].Id;
        var suppliedAgentId = _fixture.Agents["0"].Id;

        var listed = await AgentRepo(readerId).ListMetadata(_organizationId);

        Assert.Contains(listed, a => a.Id == suppliedAgentId);
    }

    /// <summary>
    /// The sibling Agent is supplied to Module0001, so a role on Module0000 must not reach it.
    /// </summary>
    [Fact]
    public async Task AModuleReaderDoesNotResolveAnAgentSuppliedToASiblingModule()
    {
        var readerId = _fixture.ScopeReaderUsers["Module0000.Reader"].Id;
        var siblingAgentId = _fixture.Agents["0Sibling"].Id;

        var listed = await AgentRepo(readerId).ListMetadata(_organizationId);

        Assert.DoesNotContain(listed, a => a.Id == siblingAgentId);
    }

    /// <summary>
    /// Runner0 is supplied to Module0000 and Runner0Sibling to Module0001, so each module's reader
    /// resolves one and not the other.
    /// </summary>
    [Fact]
    public async Task EachModuleReaderResolvesOnlyTheRunnerSuppliedToTheirModule()
    {
        var listedForModule0000 = await RunnerRepo(
            _fixture.ScopeReaderUsers["Module0000.Reader"].Id).ListMetadata(_organizationId);
        var listedForModule0001 = await RunnerRepo(
            _fixture.ScopeReaderUsers["Module0001.Reader"].Id).ListMetadata(_organizationId);

        Assert.Contains(listedForModule0000, r => r.Id == _fixture.Runners["0"].Id);
        Assert.DoesNotContain(listedForModule0000, r => r.Id == _fixture.Runners["0Sibling"].Id);

        Assert.Contains(listedForModule0001, r => r.Id == _fixture.Runners["0Sibling"].Id);
        Assert.DoesNotContain(listedForModule0001, r => r.Id == _fixture.Runners["0"].Id);
    }

    /// <summary>
    /// A metadata-only role on the module does not chain into metadata read on what is supplied
    /// there. Reading a scope is the entitlement the supply follows, and MetadataReader is not it.
    /// </summary>
    [Fact]
    public async Task AModuleMetadataReaderResolvesNothingSuppliedToThatModule()
    {
        var metadataReaderId = _fixture.ScopeReaderUsers["Module0000.MetadataReader"].Id;
        var suppliedAgentId = _fixture.Agents["0"].Id;

        Assert.Empty(await _dbContext.DerivedAgentRoleAssignments
            .Where(d => d.PrincipalId == metadataReaderId && d.AgentId == suppliedAgentId)
            .ToListAsync());

        var listed = await AgentRepo(metadataReaderId).ListMetadata(_organizationId);

        Assert.DoesNotContain(listed, a => a.Id == suppliedAgentId);
    }

    /// <summary>
    /// Metadata read is all the supply carries: the Agent itself stays unreadable.
    /// </summary>
    [Fact]
    public void TheSupplyCarriesMetadataReadAndNothingMore()
    {
        var readerId = _fixture.ScopeReaderUsers["Module0000.Reader"].Id;
        var suppliedAgentId = _fixture.Agents["0"].Id;

        Assert.True(AgentRepo(readerId).CanReadMetadata(suppliedAgentId, _organizationId));
        Assert.False(AgentRepo(readerId).CanRead(suppliedAgentId, _organizationId));
    }

    [Fact]
    public async Task TheDerivedRoleIsRecordedSeparatelyFromGrantedOnes()
    {
        var readerId = _fixture.ScopeReaderUsers["Module0000.Reader"].Id;
        var suppliedAgentId = _fixture.Agents["0"].Id;

        var derived = await _dbContext.DerivedAgentRoleAssignments
            .Where(d => d.PrincipalId == readerId
                        && d.AgentId == suppliedAgentId
                        && d.OrganizationId == _organizationId)
            .ToListAsync();

        Assert.Single(derived);
        Assert.Equal(AgentRole.MetadataReader, derived[0].RoleName);

        Assert.Empty(await _dbContext.UserAgentRoleAssignments
            .Where(ra => ra.UserId == readerId && ra.AgentId == suppliedAgentId)
            .ToListAsync());
    }

    private AgentSecuredRepository AgentRepo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new AgentSecuredRepository(
            new AgentRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new AgentRepositorySettings())),
            pp);
    }

    private RunnerSecuredRepository RunnerRepo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new RunnerRepositorySettings())),
            pp);
    }
}
