// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.TestActions;

namespace SnapCd.Server.Core.Tests.Infrastructure;

/// <summary>
/// Base class for scenario-based permission tests.
/// Test classes inherit from this and provide configuration through the constructor.
/// The test class itself becomes purely declarative - just configuration.
/// Implements 8 test methods testing both positive (Can) and negative (Cannot) authorization paths.
/// </summary>
public abstract class TestBase : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private readonly TestScenarioConfiguration _config;

    protected SnapCdDbContext DbContext = null!;
    protected ITestActions TestActions = null!;
    protected Guid PrincipalId => _config.PrincipalId;
    protected PrincipalDiscriminator Discriminator => _config.Discriminator;
    protected string NamePrefix => _config.NamePrefix;

    protected TestBase(Fixture fixture, TestScenarioConfiguration config)
    {
        _fixture = fixture;
        _config = config;
    }

    public async Task InitializeAsync()
    {
        DbContext = _fixture.CreateDbContext();
        TestActions = _config.TestActionsFactory(_fixture, DbContext);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests that the principal can get the first entity from CanGetIds.
    /// Skips if CanGetIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CanGet()
    {
        if (_config.CanGetIds.Length == 0) return;
        var entityId = _config.CanGetIds[0];
        await TestActions.CanGet(PrincipalId, Discriminator, entityId);
    }

    /// <summary>
    /// Tests that the principal cannot get the first entity from CannotGetIds.
    /// Skips if CannotGetIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CannotGet()
    {
        if (_config.CannotGetIds.Length == 0) return;
        var entityId = _config.CannotGetIds[0];
        await TestActions.CannotGet(PrincipalId, Discriminator, entityId);
    }

    /// <summary>
    /// Tests that the principal can list entities and they contain the expected entities from CanGetIds.
    /// Skips if CanGetIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CanList()
    {
        if (_config.CanGetIds.Length == 0) return;
        await TestActions.CanList(PrincipalId, Discriminator, _config.CanGetIds);
    }

    /// <summary>
    /// Tests that the principal's list does NOT contain restricted entities from CannotGetIds.
    /// Skips if CannotGetIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CannotList()
    {
        if (_config.CannotGetIds.Length == 0) return;
        await TestActions.CannotList(PrincipalId, Discriminator, _config.CannotGetIds);
    }

    /// <summary>
    /// Tests that the principal can update the first entity from CanUpdateIds.
    /// Skips if CanUpdateIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CanUpdate()
    {
        if (_config.CanUpdateIds.Length == 0) return;
        var entityId = _config.CanUpdateIds[0];
        await TestActions.CanUpdate(PrincipalId, Discriminator, entityId, NamePrefix);
    }

    /// <summary>
    /// Tests that the principal cannot update the first entity from CannotUpdateIds.
    /// Skips if CannotUpdateIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CannotUpdate()
    {
        if (_config.CannotUpdateIds.Length == 0) return;
        var entityId = _config.CannotUpdateIds[0];
        await TestActions.CannotUpdate(PrincipalId, Discriminator, entityId);
    }

    /// <summary>
    /// Tests that the principal can delete the first entity from CanDeleteIds.
    /// Skips if CanDeleteIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CanDelete()
    {
        if (_config.CanDeleteIds.Length == 0) return;
        var entityId = _config.CanDeleteIds[0];
        await TestActions.CanDelete(PrincipalId, Discriminator, entityId);
    }

    /// <summary>
    /// Tests that the principal cannot delete the first entity from CannotDeleteIds.
    /// Skips if CannotDeleteIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CannotDelete()
    {
        if (_config.CannotDeleteIds.Length == 0) return;
        var entityId = _config.CannotDeleteIds[0];
        await TestActions.CannotDelete(PrincipalId, Discriminator, entityId);
    }

    /// <summary>
    /// Tests that the principal can create in the first parent context from CanCreateParentIds.
    /// Skips if CanCreateParentIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CanCreate()
    {
        if (_config.CanCreateParentIds.Length == 0) return;
        var parentId = _config.CanCreateParentIds[0];
        await TestActions.CanCreate(PrincipalId, Discriminator, parentId, NamePrefix);
    }

    /// <summary>
    /// Tests that the principal cannot create in the first parent context from CannotCreateParentIds.
    /// Skips if CannotCreateParentIds is empty.
    /// </summary>
    [Fact]
    public virtual async Task CannotCreate()
    {
        if (_config.CannotCreateParentIds.Length == 0) return;
        var parentId = _config.CannotCreateParentIds[0];
        await TestActions.CannotCreate(PrincipalId, Discriminator, parentId);
    }

    public async Task DisposeAsync()
    {
        DbContext?.Dispose();
        await Task.CompletedTask;
    }
}