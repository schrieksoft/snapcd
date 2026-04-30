using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class OrphanedJobCleanupServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private IDbContextFactory<SnapCdDbContext> _dbContextFactory = null!;
    private OrphanedJobCleanupService _service = null!;

    public OrphanedJobCleanupServiceTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // Create a mock IDbContextFactory that returns contexts from the fixture
        var dbContextFactory = new TestDbContextFactory(_fixture);
        _dbContextFactory = dbContextFactory;
        _service = new OrphanedJobCleanupService(_dbContextFactory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListOrphanedJobs_ReturnsOrphanedApplyJob()
    {
        // Act
        var orphanedJobs = await _service.ListOrphanedJobs();

        // Assert
        var orphanedApply = _fixture.OrphanedJobTestData["OrphanedApply"];
        Assert.Contains(orphanedJobs, j => j.Id == orphanedApply.Id && j.JobType == nameof(ApplyJobSaga));
    }

    [Fact]
    public async Task ListOrphanedJobs_ReturnsOrphanedDestroyJob()
    {
        // Act
        var orphanedJobs = await _service.ListOrphanedJobs();

        // Assert
        var orphanedDestroy = _fixture.OrphanedJobTestData["OrphanedDestroy"];
        Assert.Contains(orphanedJobs, j => j.Id == orphanedDestroy.Id && j.JobType == nameof(DestroyJobSaga));
    }

    [Fact]
    public async Task ListOrphanedJobs_ExcludesJobWithMatchingSaga()
    {
        // Act
        var orphanedJobs = await _service.ListOrphanedJobs();

        // Assert
        var nonOrphanedApply = _fixture.OrphanedJobTestData["NonOrphanedApply"];
        Assert.DoesNotContain(orphanedJobs, j => j.Id == nonOrphanedApply.Id);
    }

    [Fact]
    public async Task ListOrphanedJobs_ExcludesFinalizedJobWithoutSaga()
    {
        // Act
        var orphanedJobs = await _service.ListOrphanedJobs();

        // Assert
        var finalizedNoSaga = _fixture.OrphanedJobTestData["FinalizedNoSaga"];
        Assert.DoesNotContain(orphanedJobs, j => j.Id == finalizedNoSaga.Id);
    }

    [Fact]
    public async Task ListOrphanedJobs_ReturnsCorrectOrganizationId()
    {
        // Act
        var orphanedJobs = await _service.ListOrphanedJobs();

        // Assert
        var orphanedApply = _fixture.OrphanedJobTestData["OrphanedApply"];
        var result = orphanedJobs.FirstOrDefault(j => j.Id == orphanedApply.Id);
        Assert.NotNull(result);
        Assert.Equal(orphanedApply.OrganizationId, result.OrganizationId);
    }

    [Fact]
    public async Task ListOrphanedJobs_ReturnsBothOrphanedJobs()
    {
        // Act
        var orphanedJobs = await _service.ListOrphanedJobs();

        // Assert
        var orphanedApply = _fixture.OrphanedJobTestData["OrphanedApply"];
        var orphanedDestroy = _fixture.OrphanedJobTestData["OrphanedDestroy"];

        Assert.Contains(orphanedJobs, j => j.Id == orphanedApply.Id);
        Assert.Contains(orphanedJobs, j => j.Id == orphanedDestroy.Id);
    }
}

/// <summary>
/// Test implementation of IDbContextFactory that uses the fixture's service provider
/// </summary>
public class TestDbContextFactory : IDbContextFactory<SnapCdDbContext>
{
    private readonly Fixture _fixture;

    public TestDbContextFactory(Fixture fixture)
    {
        _fixture = fixture;
    }

    public SnapCdDbContext CreateDbContext()
    {
        return _fixture.CreateDbContext();
    }
}
