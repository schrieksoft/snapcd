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
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Misc;

[Collection("NewRoleBasedSharedFixture")]
public class GenericRepoAuditFieldsTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public GenericRepoAuditFieldsTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Create_SetsCreatedBy_ToCurrentPrincipal()
    {
        // Arrange
        var principalProvider =
            _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var repo = new ModuleRepository(_dbContext, principalProvider, bus, settings);

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        // Act
        var created = await repo.Create(newModule);

        // Assert
        Assert.Equal(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, created.CreatedBy);

        // Cleanup
        await repo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Create_SetsCreatedByPrincipalDiscriminator_ToCurrentDiscriminator()
    {
        // Arrange
        var principalProvider =
            _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var repo = new ModuleRepository(_dbContext, principalProvider, bus, settings);

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule2",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        // Act
        var created = await repo.Create(newModule);

        // Assert
        Assert.Equal(AuditPrincipalDiscriminator.User, created.CreatedByPrincipalDiscriminator);

        // Cleanup
        await repo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Create_SetsCreatedDateTime_ToCurrentTime()
    {
        // Arrange
        var principalProvider =
            _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, bus, settings),
            principalProvider
        );

        var beforeCreate = DateTime.UtcNow;
        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule3",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        // Act
        var created = await repo.Create(newModule);
        var afterCreate = DateTime.UtcNow;

        // Assert
        Assert.True(created.CreatedDateTime >= beforeCreate);
        Assert.True(created.CreatedDateTime <= afterCreate);

        // Cleanup
        await repo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Create_SetsModifiedBy_ToCurrentPrincipal()
    {
        // Arrange
        var principalProvider =
            _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, bus, settings),
            principalProvider
        );

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule4",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        // Act
        var created = await repo.Create(newModule);

        // Assert
        Assert.Equal(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, created.ModifiedBy);

        // Cleanup
        await repo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Create_WithServicePrincipal_SetsCorrectDiscriminator()
    {
        // Arrange
        var principalProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id, PrincipalDiscriminator.ServicePrincipal,
            _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var repo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, principalProvider, bus, settings),
            principalProvider
        );

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule5",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        // Act
        var created = await repo.Create(newModule);

        // Assert
        Assert.Equal(AuditPrincipalDiscriminator.ServicePrincipal, created.CreatedByPrincipalDiscriminator);
        Assert.Equal(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id, created.CreatedBy);

        // Cleanup
        await repo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Update_PreservesCreatedBy()
    {
        // Arrange - Create module with one user
        var creatorProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var createRepo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, creatorProvider, bus, settings),
            creatorProvider
        );

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule6",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        var created = await createRepo.Create(newModule);
        var originalCreatedBy = created.CreatedBy;

        // Act - Update with different user
        var updaterProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Contributor].DirectUser.Id, PrincipalDiscriminator.User,
            _fixture.Organizations["0"].Id);
        var updateRepo = new ModuleRepository(_dbContext, updaterProvider, bus, settings);

        var updateModule = new Module
        {
            Id = created.Id,
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "UpdatedAuditModule",
            SourceUrl = created.SourceUrl,
            SourceRevision = created.SourceRevision,
            SourceSubdirectory = created.SourceSubdirectory
        };

        var updated = await updateRepo.Update(updateModule);

        // Assert
        Assert.Equal(originalCreatedBy, updated.CreatedBy);
        Assert.Equal(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, updated.CreatedBy);

        // Cleanup
        await createRepo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Update_PreservesCreatedByPrincipalDiscriminator()
    {
        // Arrange
        var creatorProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var createRepo =
            new ModuleRepository(_dbContext, creatorProvider, bus, settings);

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule7",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        var created = await createRepo.Create(newModule);

        // Act
        var updaterProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id, PrincipalDiscriminator.ServicePrincipal,
            _fixture.Organizations["0"].Id);
        var updateRepo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, updaterProvider, bus, settings),
            updaterProvider
        );

        var updateModule = new Module
        {
            Id = created.Id,
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "UpdatedAuditModule",
            SourceUrl = created.SourceUrl,
            SourceRevision = created.SourceRevision,
            SourceSubdirectory = created.SourceSubdirectory
        };

        var updated = await updateRepo.Update(updateModule);

        // Assert
        Assert.Equal(AuditPrincipalDiscriminator.User, updated.CreatedByPrincipalDiscriminator);

        // Cleanup
        await createRepo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Update_UpdatesModifiedBy_ToCurrentPrincipal()
    {
        // Arrange
        var creatorProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var createRepo =
            new ModuleRepository(_dbContext, creatorProvider, bus, settings);

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule8",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        var created = await createRepo.Create(newModule);

        // Act
        var updaterProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Contributor].DirectUser.Id, PrincipalDiscriminator.User,
            _fixture.Organizations["0"].Id);
        var updateRepo = new ModuleRepository(_dbContext, updaterProvider, bus, settings);

        var updateModule = new Module
        {
            Id = created.Id,
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "UpdatedAuditModule",
            SourceUrl = created.SourceUrl,
            SourceRevision = created.SourceRevision,
            SourceSubdirectory = created.SourceSubdirectory
        };

        var updated = await updateRepo.Update(updateModule);

        // Assert
        Assert.Equal(_fixture.OrganizationPrincipals["0"][OrganizationRole.Contributor].DirectUser.Id, updated.ModifiedBy);

        // Cleanup
        await createRepo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Update_UpdatesModifiedByPrincipalDiscriminator()
    {
        // Arrange
        var creatorProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var createRepo = new ModuleRepository(_dbContext, creatorProvider, bus, settings);

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule9",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        var created = await createRepo.Create(newModule);

        // Act
        var updaterProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectServicePrincipal.Id, PrincipalDiscriminator.ServicePrincipal,
            _fixture.Organizations["0"].Id);
        var updateRepo = new ModuleRepository(_dbContext, updaterProvider, bus, settings);

        var updateModule = new Module
        {
            Id = created.Id,
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "UpdatedAuditModule",
            SourceUrl = created.SourceUrl,
            SourceRevision = created.SourceRevision,
            SourceSubdirectory = created.SourceSubdirectory
        };

        var updated = await updateRepo.Update(updateModule);

        // Assert
        Assert.Equal(AuditPrincipalDiscriminator.ServicePrincipal, updated.ModifiedByPrincipalDiscriminator);

        // Cleanup
        await createRepo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }

    [Fact]
    public async Task Update_ByDifferentUser_ChangesModifiedByNotCreatedBy()
    {
        // Arrange
        var creatorProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id);
        var bus = _fixture.CreateMockBus();
        var settings = _fixture.CreateModuleSettings();
        var createRepo = new ModuleRepository(_dbContext, creatorProvider, bus, settings);

        var newModule = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "AuditTestModule10",
            SourceUrl = "https://github.com/test/audit",
            SourceRevision = "main",
            SourceSubdirectory = "terraform"
        };

        var created = await createRepo.Create(newModule);

        // Act
        var updaterProvider = _fixture.CreatePrincipalProvider(_fixture.OrganizationPrincipals["0"][OrganizationRole.Contributor].DirectUser.Id, PrincipalDiscriminator.User,
            _fixture.Organizations["0"].Id);
        var updateRepo = new ModuleSecuredRepository(
            new ModuleRepository(_dbContext, updaterProvider, bus, settings),
            updaterProvider
        );

        var updateModule = new Module
        {
            Id = created.Id,
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = "UpdatedAuditModule",
            SourceUrl = created.SourceUrl,
            SourceRevision = created.SourceRevision,
            SourceSubdirectory = created.SourceSubdirectory
        };

        var updated = await updateRepo.Update(updateModule);

        // Assert
        Assert.Equal(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, updated.CreatedBy);
        Assert.Equal(_fixture.OrganizationPrincipals["0"][OrganizationRole.Contributor].DirectUser.Id, updated.ModifiedBy);
        Assert.NotEqual(updated.CreatedBy, updated.ModifiedBy);

        // Cleanup
        await createRepo.Delete(created.Id, _fixture.Organizations["0"].Id);
    }
}