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
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

/// <summary>
/// Reusable test actions for Runner entity CRUD operations using the new ITestActionsNew interface.
/// This implementation provides explicit positive and negative test methods without shouldSucceed parameters.
/// </summary>
public class RunnerTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public RunnerTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Act
        var runners = await repo.List(_fixture.Organizations["0"].Id);

        // Assert
        foreach (var expectedId in expectedEntityIds) Assert.Contains(runners, rp => rp.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Act
        var runners = await repo.List(_fixture.Organizations["0"].Id);

        // Assert - Should NOT contain any of these entities (from other organizations)
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(runners, rp => rp.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Act
        var runner = await repo.Get(entityId, _fixture.Organizations["0"].Id);

        // Assert
        Assert.NotNull(runner);
        Assert.Equal(entityId, runner.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Find the Runner's actual organization (for cross-org tests)
        var runner = _dbContext.Runners.FirstOrDefault(rp => rp.Id == entityId);
        Assert.NotNull(runner);

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () => await repo.Get(entityId, runner.OrganizationId));
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Act
        var runner = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        var updatedName = $"{namePrefix}_Updated_{Guid.NewGuid().ToString("N")[..8]}";
        runner.Name = updatedName;
        var updated = await repo.Update(runner);

        // Assert
        Assert.Equal(updatedName, updated.Name);
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Find the Runner's actual organization (for cross-org tests)
        var runner = _dbContext.Runners.FirstOrDefault(rp => rp.Id == entityId);
        Assert.NotNull(runner);

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
        {
            var rpToUpdate = await repo.Get(entityId, runner.OrganizationId);
            rpToUpdate.Name = "ShouldNotUpdate";
            await repo.Update(rpToUpdate);
        });
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Act
        await repo.Delete(entityId, _fixture.Organizations["0"].Id);

        // Assert - Entity should no longer be accessible
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, _fixture.Organizations["0"].Id));
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Find the Runner's actual organization (for cross-org tests)
        var runner = _dbContext.Runners.FirstOrDefault(rp => rp.Id == entityId);
        Assert.NotNull(runner);

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Delete(entityId, runner.OrganizationId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        // Arrange - parentId is the organization ID for Runner
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Act
        var newRunner = new Runner
        {
            Id = Guid.NewGuid(),
            Name = $"{namePrefix}_Created_{Guid.NewGuid().ToString("N")[..8]}",
            OrganizationId = parentId
        };

        var created = await repo.Create(newRunner);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(newRunner.Name, created.Name);

        // Cleanup
        await repo.Delete(created.Id, parentId);
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        // Arrange - parentId is the organization ID for Runner
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new RunnerSecuredRepository(
            new RunnerRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new RunnerRepositorySettings())),
            principalProvider
        );

        // Act
        var newRunner = new Runner
        {
            Id = Guid.NewGuid(),
            Name = $"ShouldNotCreate_{Guid.NewGuid().ToString("N")[..8]}",
            OrganizationId = parentId
        };

        // Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () => await repo.Create(newRunner));
    }
}