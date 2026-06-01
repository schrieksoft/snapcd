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
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

public class UserOrganizationRoleAssignmentTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public UserOrganizationRoleAssignmentTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var assignments = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var expectedId in expectedEntityIds) Assert.Contains(assignments, a => a.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var assignments = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(assignments, a => a.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var assignment = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        Assert.NotNull(assignment);
        Assert.Equal(entityId, assignment.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.UserOrganizationRoleAssignments.FirstOrDefaultAsync(a => a.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var assignment = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        var originalRole = assignment.RoleName;
        var newRole = originalRole == OrganizationRole.Owner ? OrganizationRole.Contributor : OrganizationRole.Owner;
        assignment.RoleName = newRole;
        var updated = await repo.Update(assignment);
        Assert.Equal(newRole, updated.RoleName);

        // Restore original role
        assignment.RoleName = originalRole;
        await repo.Update(assignment);
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.UserOrganizationRoleAssignments.FirstOrDefaultAsync(a => a.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        await repo.Delete(entityId, _fixture.Organizations["0"].Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, _fixture.Organizations["0"].Id));
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var entity = await _dbContext.UserOrganizationRoleAssignments.FirstOrDefaultAsync(a => a.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Delete(entityId, organizationId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        // Note: UserOrganizationRoleAssignment doesn't have a parent entity - assignments are scoped directly to organization
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var newAssignment = new UserOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            UserId = _fixture.NoPermissionUser.Id,
            RoleName = OrganizationRole.Reader
        };
        var created = await repo.Create(newAssignment);
        Assert.NotNull(created);
        Assert.Equal(newAssignment.RoleName, created.RoleName);

        // Cleanup
        await repo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        // Note: For role assignments, we test cross-org isolation by using NoPermissionUser from Org 1
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new UserOrganizationRoleAssignmentSecuredRepository(
            new UserOrganizationRoleAssignmentRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
                Microsoft.Extensions.Options.Options.Create(new UserOrganizationRoleAssignmentRepositorySettings())),
            principalProvider
        );
        var newAssignment = new UserOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["1"].Id,
            UserId = _fixture.NoPermissionUser.Id,
            RoleName = OrganizationRole.Reader
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Create(newAssignment));
    }
}