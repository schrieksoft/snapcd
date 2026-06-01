// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

/// <summary>
/// Test actions for NamespaceParamFromSecret entity.
/// This is a READ-ONLY entity for RunnerRunner role (no Create/Update/Delete permissions).
/// </summary>
public class NamespaceInputFromSecretTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public NamespaceInputFromSecretTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new NamespaceInputFromSecretSecuredRepository<NamespaceParamFromSecret>(
            new NamespaceInputFromSecretRepository<NamespaceParamFromSecret>(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new NamespaceInputFromSecretRepositorySettings())),
            principalProvider
        );
        var namespaceInputs = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var expectedId in expectedEntityIds) Assert.Contains(namespaceInputs, ni => ni.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new NamespaceInputFromSecretSecuredRepository<NamespaceParamFromSecret>(
            new NamespaceInputFromSecretRepository<NamespaceParamFromSecret>(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new NamespaceInputFromSecretRepositorySettings())),
            principalProvider
        );
        var namespaceInputs = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(namespaceInputs, ni => ni.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new NamespaceInputFromSecretSecuredRepository<NamespaceParamFromSecret>(
            new NamespaceInputFromSecretRepository<NamespaceParamFromSecret>(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new NamespaceInputFromSecretRepositorySettings())),
            principalProvider
        );
        var namespaceInput = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        Assert.NotNull(namespaceInput);
        Assert.Equal(entityId, namespaceInput.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Find which organization this entity belongs to
        var entity = await _dbContext.NamespaceInputs.OfType<NamespaceParamFromSecret>().FirstOrDefaultAsync(ni => ni.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new NamespaceInputFromSecretSecuredRepository<NamespaceParamFromSecret>(
            new NamespaceInputFromSecretRepository<NamespaceParamFromSecret>(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new NamespaceInputFromSecretRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    // Read-only entity - no Update/Delete/Create permissions for RunnerRunner
    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        throw new NotImplementedException("NamespaceParamFromSecret is read-only for RunnerRunner role");
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        throw new NotImplementedException("NamespaceParamFromSecret is read-only for RunnerRunner role");
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        throw new NotImplementedException("NamespaceParamFromSecret is read-only for RunnerRunner role");
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        throw new NotImplementedException("NamespaceParamFromSecret is read-only for RunnerRunner role");
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        throw new NotImplementedException("NamespaceParamFromSecret is read-only for RunnerRunner role");
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        throw new NotImplementedException("NamespaceParamFromSecret is read-only for RunnerRunner role");
    }
}