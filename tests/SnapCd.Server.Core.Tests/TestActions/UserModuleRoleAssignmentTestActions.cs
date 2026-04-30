using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

public class UserModuleRoleAssignmentTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public UserModuleRoleAssignmentTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var assignments = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var expectedId in expectedEntityIds) Assert.Contains(assignments, a => a.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var assignments = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(assignments, a => a.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var assignment = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        Assert.NotNull(assignment);
        Assert.Equal(entityId, assignment.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.UserModuleRoleAssignments.FirstOrDefaultAsync(a => a.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var assignment = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        var originalRole = assignment.RoleName;
        var newRole = originalRole == ModuleRole.Owner ? ModuleRole.Contributor : ModuleRole.Owner;
        assignment.RoleName = newRole;
        var updated = await repo.Update(assignment);
        Assert.Equal(newRole, updated.RoleName);

        // Restore original role
        assignment.RoleName = originalRole;
        await repo.Update(assignment);
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.UserModuleRoleAssignments.FirstOrDefaultAsync(a => a.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        await repo.Delete(entityId, _fixture.Organizations["0"].Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, _fixture.Organizations["0"].Id));
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.UserModuleRoleAssignments.FirstOrDefaultAsync(a => a.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Delete(entityId, organizationId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        // parentId is the moduleId for module role assignments
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var newAssignment = new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            ModuleId = parentId,
            UserId = _fixture.NoPermissionUser.Id,
            RoleName = ModuleRole.Reader
        };
        var created = await repo.Create(newAssignment);
        Assert.NotNull(created);
        Assert.Equal(newAssignment.RoleName, created.RoleName);

        // Cleanup
        await repo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        // parentId is the moduleId from Org "1" for cross-org isolation testing
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserModuleRoleAssignmentSecuredRepository(
            new UserModuleRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserModuleRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var newAssignment = new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["1"].Id,
            ModuleId = parentId,
            UserId = _fixture.NoPermissionUser.Id,
            RoleName = ModuleRole.Reader
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Create(newAssignment));
    }
}