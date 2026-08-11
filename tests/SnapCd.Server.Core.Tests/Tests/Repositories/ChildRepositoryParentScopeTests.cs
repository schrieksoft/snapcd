// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.AgentSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Repositories;

/// <summary>
/// ListByParentId resolves its filter column from the repository's base class, so a repository
/// declared on the wrong base silently filters on the wrong column and returns nothing rather
/// than failing.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ChildRepositoryParentScopeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public ChildRepositoryParentScopeTests(Fixture fixture) => _fixture = fixture;

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
    public async Task ListByParentIdScopesToTheAgentRatherThanTheOrganization()
    {
        var orgId = _fixture.Organizations["0"].Id;
        var agentId = _fixture.Agents["0"].Id;
        var siblingId = _fixture.Agents["0Sibling"].Id;

        var mine = await Repo().ListByParentId(agentId, orgId);
        var theirs = await Repo().ListByParentId(siblingId, orgId);

        Assert.All(mine, s => Assert.Equal(agentId, s.AgentId));
        Assert.All(theirs, s => Assert.Equal(siblingId, s.AgentId));

        // Both agents hold a supply in the same organization, so an OrganizationId filter would
        // return every row to both of them.
        Assert.Contains(mine, s => s.Id == _fixture.AgentModuleSupplies["0"].Id);
        Assert.Contains(theirs, s => s.Id == _fixture.AgentModuleSupplies["0Sibling"].Id);
        Assert.DoesNotContain(mine, s => s.Id == _fixture.AgentModuleSupplies["0Sibling"].Id);
        Assert.DoesNotContain(theirs, s => s.Id == _fixture.AgentModuleSupplies["0"].Id);
    }

    [Fact]
    public async Task ListByParentIdReturnsNothingForAnAgentWithNoSupplies()
    {
        var orgId = _fixture.Organizations["0"].Id;

        Assert.Empty(await Repo().ListByParentId(Guid.NewGuid(), orgId));
    }

    /// <summary>
    /// Agents, Runners, Integrations and StateStores own children of their own (supplies, role
    /// assignments), which are scoped by the owner and not by the organization they sit in.
    /// </summary>
    [Fact]
    public void OwnerScopedChildRepositoriesDeclareTheMatchingBase()
    {
        var owners = new (Type Marker, string Base)[]
        {
            (typeof(IAgentChild), "GenericAgentChildRepository`6"),
            (typeof(IRunnerChild), "GenericRunnerChildRepository`6"),
            (typeof(IIntegrationChild), "GenericIntegrationChildRepository`6"),
            (typeof(IStateStoreChild), "GenericStateStoreChildRepository`6"),
        };

        // An entity that is also scoped to a Stack, Namespace or Module belongs to that scope.
        var scopes = new[] { typeof(IStackChild), typeof(INamespaceChild), typeof(IModuleChild) };

        var offenders = new List<string>();

        foreach (var repo in typeof(GenericRepository<,,,,,>).Assembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
                     .Where(t => t.Name.EndsWith("Repository") && !t.Name.EndsWith("SecuredRepository")))
        {
            var entity = EntityOf(repo);
            if (entity is null) continue;
            if (scopes.Any(s => s.IsAssignableFrom(entity))) continue;

            foreach (var (marker, expected) in owners)
            {
                if (!marker.IsAssignableFrom(entity)) continue;

                var actual = BaseNames(repo);
                if (!actual.Contains(expected))
                    offenders.Add($"{repo.Name} ({entity.Name}) expected {expected}, found {string.Join(" -> ", actual)}");
                break;
            }
        }

        Assert.Empty(offenders);
    }

    private static Type? EntityOf(Type repo)
    {
        for (var t = repo.BaseType; t is not null; t = t.BaseType)
            if (t.IsGenericType && t.GetGenericArguments().Length >= 1)
            {
                var candidate = t.GetGenericArguments()[0];
                if (typeof(IEntity).IsAssignableFrom(candidate)) return candidate;
            }

        return null;
    }

    private static List<string> BaseNames(Type repo)
    {
        var names = new List<string>();
        for (var t = repo.BaseType; t is not null; t = t.BaseType)
            names.Add(t.IsGenericType ? t.GetGenericTypeDefinition().Name : t.Name);
        return names;
    }

    private AgentModuleSupplyRepository Repo()
    {
        var pp = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            PrincipalDiscriminator.User,
            _fixture.Organizations["0"].Id);

        return new AgentModuleSupplyRepository(_dbContext, pp, _fixture.CreateMockBus(),
            Options.Create(new AgentModuleSupplyRepositorySettings()));
    }
}
