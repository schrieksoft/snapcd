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
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Variables;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

public class InputTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public InputTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );
        var inputs = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var expectedId in expectedEntityIds) Assert.Contains(inputs, i => i.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );
        var inputs = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(inputs, i => i.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );
        var input = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        Assert.NotNull(input);
        Assert.Equal(entityId, input.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.Variables.FirstOrDefaultAsync(i => i.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        // Input is IMMUTABLE - updates are not allowed
        // This test verifies that even with permission, update is blocked
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );

        // Verify CanUpdate returns false for immutable entities
        Assert.False(repo.CanUpdate(entityId, _fixture.Organizations["0"].Id));
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Input is IMMUTABLE - updates are not allowed regardless of permissions
        var entity = await _dbContext.Variables.FirstOrDefaultAsync(i => i.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );

        // Verify CanUpdate returns false
        Assert.False(repo.CanUpdate(entityId, organizationId));
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );
        await repo.Delete(entityId, _fixture.Organizations["0"].Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, _fixture.Organizations["0"].Id));
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.Variables.FirstOrDefaultAsync(i => i.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Delete(entityId, organizationId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        // NOTE: Input can only be created by Runner roles, NOT by OrganizationOwner
        // This should always throw for OrganizationOwner
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );
        var newInput = new Variable
        {
            Id = Guid.NewGuid(),
            Name = $"{namePrefix}_Create_{Guid.NewGuid().ToString("N")[..8]}",
            Type = "string",
            VariableSetId = parentId,
            OrganizationId = _fixture.Organizations["0"].Id
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Create(newInput));
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        // NOTE: Input can only be created by Runner roles, NOT by OrganizationOwner
        var variableSet = await _dbContext.VariableSets.FirstOrDefaultAsync(ins => ins.Id == parentId);
        var organizationId = variableSet?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new VariableSecuredRepository(
            new VariableRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new VariableRepositorySettings())),
            principalProvider
        );
        var newInput = new Variable
        {
            Id = Guid.NewGuid(),
            Name = $"CannotCreate_{Guid.NewGuid().ToString("N")[..8]}",
            Type = "string",
            VariableSetId = parentId,
            OrganizationId = organizationId
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Create(newInput));
    }
}