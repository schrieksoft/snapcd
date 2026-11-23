// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.ParamResolver.Helpers;
using SnapCd.Server.Core.Services.Nonsecured.ResolvedConfiguration;
using SnapCd.Server.Core.Services.Secured.Crud.Secrets;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// Rows a nesting test builds for itself in organization "0", removed again on dispose. The seeded
/// Namespaces are shared and flat, so a test that needs a tree makes its own.
/// </summary>
public sealed class NestedScenario(Fixture fixture) : IAsyncDisposable
{
    private readonly List<Guid> _namespaces = [];
    private readonly List<Guid> _modules = [];
    private readonly List<Guid> _users = [];
    private readonly List<Guid> _runners = [];
    private readonly List<Guid> _servicePrincipals = [];

    public Guid OrganizationId => fixture.Organizations["0"].Id;

    public async Task<Namespace> AddNamespace(string stackKey, Guid? parentId = null, Action<Namespace>? configure = null)
    {
        var ns = new Namespace
        {
            Id = Guid.NewGuid(),
            Name = $"nested_{Guid.NewGuid():N}",
            StackId = fixture.Stacks[stackKey].Id,
            OrganizationId = OrganizationId,
            ParentNamespaceId = parentId,
            CreatedDateTime = DateTime.UtcNow
        };
        configure?.Invoke(ns);

        await using var db = fixture.CreateDbContext();
        db.Namespaces.Add(ns);
        await db.SaveChangesAsync();

        _namespaces.Add(ns.Id);
        return ns;
    }

    /// <summary>A Namespace the code under test created, so that it is removed with the rest.</summary>
    public void Track(Namespace ns) => _namespaces.Add(ns.Id);

    public async Task<Module> AddModule(Guid namespaceId, Action<Module>? configure = null)
    {
        await using var db = fixture.CreateDbContext();
        var module = fixture.CreateTestModule($"nested_{Guid.NewGuid():N}", "000", db);
        module.NamespaceId = namespaceId;
        configure?.Invoke(module);
        await db.SaveChangesAsync();

        _modules.Add(module.Id);
        return module;
    }

    public async Task<User> AddUser()
    {
        await using var db = fixture.CreateDbContext();
        var user = fixture.CreateTestUser($"nested-{Guid.NewGuid():N}@test.com", OrganizationId, db);
        await db.SaveChangesAsync();

        _users.Add(user.Id);
        return user;
    }

    public async Task<Runner> AddRunner()
    {
        await using var db = fixture.CreateDbContext();
        var runner = fixture.CreateTestRunner($"nested-{Guid.NewGuid():N}", OrganizationId, db);
        await db.SaveChangesAsync();

        _runners.Add(runner.Id);
        _servicePrincipals.Add(runner.ServicePrincipalId);
        return runner;
    }

    /// <summary>An Output Set holding one literal Output, "vpc_id", and one secret Output, "db_password".</summary>
    public async Task<List<Output>> AddOutputs(Guid moduleId)
    {
        var set = new OutputSet
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = OrganizationId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Checksum = $"nested-{Guid.NewGuid():N}"
        };

        List<Output> outputs =
        [
            new LiteralOutput
            {
                Id = Guid.NewGuid(), Name = "vpc_id", Type = "string", Value = "test-value",
                OutputSetId = set.Id, OrganizationId = OrganizationId
            },
            new SecretOutput
            {
                Id = Guid.NewGuid(), Name = "db_password", Type = "string", RemoteSecretName = "db-password-secret",
                OutputSetId = set.Id, OrganizationId = OrganizationId
            }
        ];

        await using var db = fixture.CreateDbContext();
        db.OutputSets.Add(set);
        db.Outputs.AddRange(outputs);
        await db.SaveChangesAsync();

        return outputs;
    }

    public OutputRestrictionChecker Checker() => new(fixture.CreateDbContext());

    public IPrincipalProvider Owner() => fixture.CreatePrincipalProvider(
        fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
        PrincipalDiscriminator.User,
        OrganizationId);

    public NamespaceSecuredRepository NamespaceRepositoryFor(Guid userId)
    {
        var principal = fixture.CreatePrincipalProvider(userId, PrincipalDiscriminator.User, OrganizationId);

        return new NamespaceSecuredRepository(
            new NamespaceRepository(
                fixture.CreateDbContext(), principal, fixture.CreateMockBus(),
                Options.Create(new NamespaceRepositorySettings())),
            principal);
    }

    /// <summary>The resolution service, with the vault read replaced by a fixed value.</summary>
    public ResolvedConfigurationService Resolver()
    {
        var secretService = new Mock<SecretService>(
            null!, null!, null!, null!, null!, null!, Options.Create(new SecretStoreSettings()));
        secretService
            .Setup(s => s.GetRemoteNonsecured(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync("secret-value");

        var secretServiceFactory = new Mock<SecretServiceFactory>(null!, null!, null!, null!, null!, null!, null!);
        secretServiceFactory
            .Setup(f => f.Create(It.IsAny<IPrincipalProvider?>()))
            .Returns(secretService.Object);

        var owner = Owner();
        var bus = fixture.CreateMockBus();

        return new ResolvedConfigurationService(
            fixture.CreateDbContext(),
            new StackSecretRepository(fixture.CreateDbContext(), owner, bus, Options.Create(new StackSecretRepositorySettings())),
            new NamespaceSecretRepository(fixture.CreateDbContext(), owner, bus, Options.Create(new NamespaceSecretRepositorySettings())),
            new ModuleSecretRepository(fixture.CreateDbContext(), owner, bus, Options.Create(new ModuleSecretRepositorySettings())),
            secretServiceFactory.Object);
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = fixture.CreateDbContext();

        await db.UserStackRoleAssignments.Where(x => _users.Contains(x.UserId)).ExecuteDeleteAsync();
        await db.UserNamespaceRoleAssignments.Where(x => _users.Contains(x.UserId)).ExecuteDeleteAsync();
        await db.UserModuleRoleAssignments.Where(x => _users.Contains(x.UserId)).ExecuteDeleteAsync();

        await db.ClearRowsBlockingModuleDelete(m => _modules.Contains(m.Id), OrganizationId);
        await db.Modules.Where(m => _modules.Contains(m.Id)).ExecuteDeleteAsync();

        await db.ClearRowsBlockingNamespaceDelete(n => _namespaces.Contains(n.Id), OrganizationId);

        // A parent is restricted from its children, so the deepest go first.
        foreach (var id in _namespaces.AsEnumerable().Reverse())
            await db.Namespaces.Where(n => n.Id == id).ExecuteDeleteAsync();

        await db.Runners.Where(r => _runners.Contains(r.Id)).ExecuteDeleteAsync();
        await db.ServicePrincipals.Where(s => _servicePrincipals.Contains(s.Id)).ExecuteDeleteAsync();

        await db.OrganizationUsers.Where(x => _users.Contains(x.UserId)).ExecuteDeleteAsync();
        await db.Users.Where(u => _users.Contains(u.Id)).ExecuteDeleteAsync();
    }
}
