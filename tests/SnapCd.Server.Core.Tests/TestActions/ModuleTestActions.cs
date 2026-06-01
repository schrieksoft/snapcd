// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

/// <summary>
/// New implementation of Module permission test actions using explicit positive/negative methods.
/// Each method tests a single operation outcome (success or failure).
/// </summary>
public class ModuleTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public ModuleTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Act
        var modules = await repo.List(orgId);

        // Assert - Should contain all expected entities
        foreach (var expectedId in expectedEntityIds) Assert.Contains(modules, m => m.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Act
        var modules = await repo.List(orgId);

        // Assert - Should NOT contain any of these entities
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(modules, m => m.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Act
        var module = await repo.Get(entityId, orgId);

        // Assert
        Assert.NotNull(module);
        Assert.Equal(entityId, module.Id);
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Get entity directly from DbContext to bypass read permission check (using composite key)
        var module = await _dbContext.Modules.FindAsync(entityId, orgId);
        Assert.NotNull(module);

        // Act - Only test update permission
        var updatedName = $"{namePrefix}_{Guid.NewGuid().ToString("N")[..8]}";
        module.Name = updatedName;
        var updated = await repo.Update(module);

        // Assert
        Assert.Equal(updatedName, updated.Name);
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Act
        await repo.Delete(entityId, orgId);

        // Assert - Verify entity no longer accessible
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, orgId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Look up the namespace to get its organization and find appropriate Runner
        var targetNamespace = await _dbContext.Namespaces.FindAsync(parentId, orgId);
        Assert.NotNull(targetNamespace);

        var runner = _dbContext.Runners.First(rp => rp.OrganizationId == targetNamespace.OrganizationId);

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            Name = $"{namePrefix}_{Guid.NewGuid().ToString("N")[..8]}",
            NamespaceId = parentId, // For modules, parentId is the namespace ID
            RunnerId = runner.Id,
            OrganizationId = orgId,
            SourceUrl = "https://github.com/test/new",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        // Act
        var created = await repo.Create(newModule);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(newModule.Name, created.Name);
        Assert.Equal(parentId, created.NamespaceId);

        // Cleanup
        await repo.Delete(created.Id, orgId);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () => await repo.Get(entityId, orgId));
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Find the module's actual organization (could be different from principal's org for cross-org tests)
        var module = _dbContext.Modules.FirstOrDefault(m => m.Id == entityId);
        Assert.NotNull(module);

        // Act & Assert - Should fail when trying to update (not on read)
        module.Name = "ShouldNotUpdate";
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Update(module));
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () => await repo.Delete(entityId, orgId));
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        // Arrange
        var orgId = _fixture.Organizations["0"].Id;
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, orgId);
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider
        );

        // Look up the namespace to get its organization
        var targetNamespace = await _dbContext.Namespaces.FindAsync(parentId, _fixture.Organizations["1"].Id);
        Assert.NotNull(targetNamespace);

        // Find appropriate Runner for the target organization
        var runner = _dbContext.Runners.First(rp => rp.OrganizationId == targetNamespace.OrganizationId);

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            Name = $"ShouldNotCreate_{Guid.NewGuid().ToString("N")[..8]}",
            NamespaceId = parentId,
            RunnerId = runner.Id,
            OrganizationId = targetNamespace.OrganizationId, // Use namespace's organization
            SourceUrl = "https://github.com/test/restricted",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () => await repo.Create(newModule));
    }
}