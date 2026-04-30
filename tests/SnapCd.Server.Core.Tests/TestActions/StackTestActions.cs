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
/// Reusable test actions for Stack entity CRUD operations using the new ITestActionsNew interface.
/// This implementation provides explicit positive and negative test methods without shouldSucceed parameters.
/// </summary>
public class StackTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public StackTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Act
        var stacks = await repo.List(_fixture.Organizations["0"].Id);

        // Assert
        foreach (var expectedId in expectedEntityIds) Assert.Contains(stacks, s => s.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Act
        var stacks = await repo.List(_fixture.Organizations["0"].Id);

        // Assert - Should NOT contain any of these entities (from other organizations)
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(stacks, s => s.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Act
        var stack = await repo.Get(entityId, _fixture.Organizations["0"].Id);

        // Assert
        Assert.NotNull(stack);
        Assert.Equal(entityId, stack.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Find the stack's actual organization (for cross-org tests)
        var stack = _dbContext.Stacks.FirstOrDefault(s => s.Id == entityId);
        Assert.NotNull(stack);

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () => await repo.Get(entityId, stack.OrganizationId));
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Act
        var stack = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        var updatedName = $"{namePrefix}_Updated_{Guid.NewGuid().ToString("N")[..8]}";
        stack.Name = updatedName;
        var updated = await repo.Update(stack);

        // Assert
        Assert.Equal(updatedName, updated.Name);
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Find the stack's actual organization (for cross-org tests)
        var stack = _dbContext.Stacks.FirstOrDefault(s => s.Id == entityId);
        Assert.NotNull(stack);

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
        {
            var stackToUpdate = await repo.Get(entityId, stack.OrganizationId);
            stackToUpdate.Name = "ShouldNotUpdate";
            await repo.Update(stackToUpdate);
        });
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
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
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Find the stack's actual organization (for cross-org tests)
        var stack = _dbContext.Stacks.FirstOrDefault(s => s.Id == entityId);
        Assert.NotNull(stack);

        // Act & Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Delete(entityId, stack.OrganizationId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        // Arrange - parentId is the organization ID for Stack
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Act
        var newStack = new Stack
        {
            Id = Guid.NewGuid(),
            Name = $"{namePrefix}_Created_{Guid.NewGuid().ToString("N")[..8]}",
            OrganizationId = parentId
        };

        var created = await repo.Create(newStack);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(newStack.Name, created.Name);

        // Cleanup
        await repo.Delete(created.Id, parentId);
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        // Arrange - parentId is the organization ID for Stack
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new StackSecuredRepository(
            new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new StackRepositorySettings())),
            principalProvider
        );

        // Act
        var newStack = new Stack
        {
            Id = Guid.NewGuid(),
            Name = $"ShouldNotCreate_{Guid.NewGuid().ToString("N")[..8]}",
            OrganizationId = parentId
        };

        // Assert
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () => await repo.Create(newStack));
    }
}