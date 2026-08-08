// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Permissions;

/// <summary>
/// ListHasRunJobPermission answers "may this principal run a job on this module". The answer has
/// to come from the module's role assignments, not from its job history: a module that has never
/// run a job is exactly the case the stack-wide apply and destroy actions have to report on.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class RunJobPermissionTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public RunJobPermissionTests(Fixture fixture)
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

    // Module0000 has seeded ModuleJobs; Module0001 has none. Both sit in Namespace000, so an
    // Owner's permission on them is identical and any difference is the job history leaking in.

    [Fact]
    public async Task Owner_HasPermission_OnModuleWithJobHistory()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var result = await ListPermissions(owner.Id, _fixture.Modules["0000"].Id, _fixture.Namespaces["000"].Id);

        Assert.True(Assert.Single(result).HasPermission);
    }

    [Fact]
    public async Task Owner_HasPermission_OnModuleWithNoJobHistory()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var result = await ListPermissions(owner.Id, _fixture.Modules["0001"].Id, _fixture.Namespaces["000"].Id);

        Assert.True(Assert.Single(result).HasPermission);
    }

    [Fact]
    public async Task JobHistory_DoesNotChangeTheAnswer()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var result = await ListPermissions(
            owner.Id,
            (_fixture.Modules["0000"].Id, _fixture.Namespaces["000"].Id),
            (_fixture.Modules["0001"].Id, _fixture.Namespaces["000"].Id));

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.HasPermission));
    }

    [Fact]
    public async Task EveryModuleAskedAboutGetsAnAnswer()
    {
        // The callers read the result with GetValueOrDefault(id, false), so a module missing from
        // the result is silently reported as a permission failure.
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var asked = new[]
        {
            (_fixture.Modules["0000"].Id, _fixture.Namespaces["000"].Id),
            (_fixture.Modules["0001"].Id, _fixture.Namespaces["000"].Id),
            (_fixture.Modules["0010"].Id, _fixture.Namespaces["001"].Id)
        };

        var result = await ListPermissions(owner.Id, asked);

        Assert.Equal(asked.Length, result.Count);
        foreach (var (moduleId, _) in asked)
            Assert.Contains(result, p => p.ModuleId == moduleId);
    }

    [Fact]
    public async Task NoRoleAnywhere_HasNoPermission_OnModuleWithNoJobHistory()
    {
        // The control: absence of job rows must not be the thing that grants or denies.
        var result = await ListPermissions(
            _fixture.NoPermissionUser.Id,
            _fixture.Modules["0001"].Id,
            _fixture.Namespaces["000"].Id);

        Assert.False(Assert.Single(result).HasPermission);
    }

    [Fact]
    public async Task ModuleRole_GrantsPermission_OnModuleWithNoJobHistory()
    {
        // A module-scoped assignment has to reach a module that has never run a job.
        var principal = _fixture.ScopeReaderUsers["Module0000.Reader"];
        var result = await ListPermissions(principal.Id, _fixture.Modules["0000"].Id, _fixture.Namespaces["000"].Id);

        // Reader is not in RunJobPermissionMap.ModuleRoles, so this is a denial by role, not by
        // missing history — the assertion pins which of the two is being measured.
        Assert.False(Assert.Single(result).HasPermission);
    }

    [Fact]
    public async Task GroupMembership_GrantsPermission_OnModuleWithNoJobHistory()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].GroupUser;
        var result = await ListPermissions(owner.Id, _fixture.Modules["0001"].Id, _fixture.Namespaces["000"].Id);

        Assert.True(Assert.Single(result).HasPermission);
    }

    [Fact]
    public async Task NestedGroupMembership_GrantsPermission_OnModuleWithNoJobHistory()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].NestedGroupUser;
        var result = await ListPermissions(owner.Id, _fixture.Modules["0001"].Id, _fixture.Namespaces["000"].Id);

        Assert.True(Assert.Single(result).HasPermission);
    }

    private Task<List<RunJobPermission>> ListPermissions(Guid principalId, Guid moduleId, Guid namespaceId)
        => ListPermissions(principalId, (moduleId, namespaceId));

    private async Task<List<RunJobPermission>> ListPermissions(Guid principalId, params (Guid ModuleId, Guid NamespaceId)[] modules)
    {
        var orgId = _fixture.Organizations["0"].Id;
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, orgId);
        var repo = new ModuleJobSecuredRepository(
            new ModuleJobRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new ModuleJobRepositorySettings())),
            pp);

        var toCheck = modules
            .Select(m => new ModuleNamespaceIdTuple { ModuleId = m.ModuleId, NamespaceId = m.NamespaceId })
            .ToList();

        return await repo.ListHasRunJobPermission(toCheck, orgId);
    }
}
