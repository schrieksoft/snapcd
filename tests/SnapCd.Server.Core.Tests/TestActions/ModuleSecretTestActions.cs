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
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

public class ModuleSecretTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public ModuleSecretTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        var secrets = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var expectedId in expectedEntityIds) Assert.Contains(secrets, s => s.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        var secrets = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(secrets, s => s.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        var secret = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        Assert.NotNull(secret);
        Assert.Equal(entityId, secret.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.ModuleSecrets.FirstOrDefaultAsync(s => s.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        var secret = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        var updatedName = $"{namePrefix}_Updated_{Guid.NewGuid().ToString("N")[..8]}";
        secret.Name = updatedName;
        var updated = await repo.Update(secret);
        Assert.Equal(updatedName, updated.Name);
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.ModuleSecrets.FirstOrDefaultAsync(s => s.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        await repo.Delete(entityId, _fixture.Organizations["0"].Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, _fixture.Organizations["0"].Id));
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.ModuleSecrets.FirstOrDefaultAsync(s => s.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Delete(entityId, organizationId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        var newSecret = new ModuleSecret
        {
            Id = Guid.NewGuid(),
            Name = $"{namePrefix}_Create_{Guid.NewGuid().ToString("N")[..8]}",
            ModuleId = parentId,
            OrganizationId = _fixture.Organizations["0"].Id
        };
        var created = await repo.Create(newSecret);
        Assert.NotNull(created);
        Assert.Equal(newSecret.Name, created.Name);

        // Cleanup
        await repo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Id == parentId);
        var organizationId = module?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new ModuleSecretRepositorySettings())),
            principalProvider
        );
        var newSecret = new ModuleSecret
        {
            Id = Guid.NewGuid(),
            Name = $"CannotCreate_{Guid.NewGuid().ToString("N")[..8]}",
            ModuleId = parentId,
            OrganizationId = organizationId
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Create(newSecret));
    }
}