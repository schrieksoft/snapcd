// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Stack = SnapCd.Server.Core.Entities.Definition.Stack;

namespace SnapCd.Server.Core.Tests.Tests.Misc;

/// <summary>
/// Names end up in URL path segments (/Stack/{name}, /Namespace/{stack}/{name},
/// /Module/{stack}/{ns}/{name}) and Terraform state paths, so the repositories must reject
/// anything outside [a-zA-Z0-9._-]. These tests prove invalid names never reach the database.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class NameValidationTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;

    public NameValidationTests(Fixture fixture)
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

    public static TheoryData<string> InvalidNames => new()
    {
        "has space",
        "a/b",
        "a?b",
        "a#b",
        "a%b",
        ".leading-dot",
        "trailing-dash-",
        "_leading-underscore",
        "über-module",
        " ",
        ""
    };

    public static TheoryData<string> ValidNames => new()
    {
        "a",
        "prod-eu",
        "my_module.v1",
        "Stack00X",
        "0numeric"
    };

    private StackRepository CreateStackRepository()
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            PrincipalDiscriminator.User,
            _fixture.Organizations["0"].Id);
        return new StackRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
            Options.Create(new StackRepositorySettings()), _fixture.CreateMockQuotaService());
    }

    private NamespaceRepository CreateNamespaceRepository()
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            PrincipalDiscriminator.User,
            _fixture.Organizations["0"].Id);
        return new NamespaceRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
            _fixture.CreateNamespaceSettings());
    }

    private ModuleRepository CreateModuleRepository()
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            PrincipalDiscriminator.User,
            _fixture.Organizations["0"].Id);
        return new ModuleRepository(_dbContext, principalProvider, _fixture.CreateMockBus(),
            _fixture.CreateModuleSettings());
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public async Task StackCreate_WithInvalidName_Throws_AndDoesNotPersist(string badName)
    {
        var repo = CreateStackRepository();
        var stack = new Stack
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            Name = badName
        };

        await Assert.ThrowsAsync<InvalidNameException>(() => repo.Create(stack));

        Assert.False(await _dbContext.Stacks.AnyAsync(s => s.Id == stack.Id));
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public async Task NamespaceCreate_WithInvalidName_Throws_AndDoesNotPersist(string badName)
    {
        var repo = CreateNamespaceRepository();
        var ns = new Namespace
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            StackId = _fixture.Stacks["00"].Id,
            Name = badName
        };

        await Assert.ThrowsAsync<InvalidNameException>(() => repo.Create(ns));

        Assert.False(await _dbContext.Namespaces.AnyAsync(n => n.Id == ns.Id));
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public async Task ModuleCreate_WithInvalidName_Throws_AndDoesNotPersist(string badName)
    {
        var repo = CreateModuleRepository();
        var module = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            NamespaceId = _fixture.Namespaces["000"].Id,
            RunnerId = _fixture.Runners["0"].Id,
            Name = badName,
            SourceUrl = "https://github.com/test/name-validation",
            SourceRevision = "main",
            SourceSubdirectory = ""
        };

        await Assert.ThrowsAsync<InvalidNameException>(() => repo.Create(module));

        Assert.False(await _dbContext.Modules.AnyAsync(m => m.Id == module.Id));
    }

    [Fact]
    public async Task StackUpdate_ToInvalidName_Throws_AndKeepsOldName()
    {
        var repo = CreateStackRepository();
        var stack = await repo.Create(new Stack
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            Name = "rename-victim"
        });

        try
        {
            stack.Name = "renamed/with/slashes";
            await Assert.ThrowsAsync<InvalidNameException>(() => repo.Update(stack));

            // A fresh context proves what actually reached the database
            await using var verifyContext = _fixture.CreateDbContext();
            var persisted = await verifyContext.Stacks.SingleAsync(s => s.Id == stack.Id);
            Assert.Equal("rename-victim", persisted.Name);
        }
        finally
        {
            await _dbContext.Stacks.Where(s => s.Id == stack.Id).ExecuteDeleteAsync();
        }
    }

    [Theory]
    [MemberData(nameof(ValidNames))]
    public async Task StackCreate_WithValidName_Succeeds(string goodName)
    {
        var repo = CreateStackRepository();
        var stack = await repo.Create(new Stack
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            Name = goodName
        });

        try
        {
            Assert.True(await _dbContext.Stacks.AnyAsync(s => s.Id == stack.Id));
        }
        finally
        {
            await _dbContext.Stacks.Where(s => s.Id == stack.Id).ExecuteDeleteAsync();
        }
    }

    // Organization creation/rename runs through the same NameValidator.EnsureValid call in
    // OrganizationSystemRepository (CreateWithOwner / ExecuteUpdate); the rule itself is
    // covered here without spinning up that repository's heavier dependency graph.
    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validator_RejectsInvalidNames(string badName)
    {
        Assert.Throws<InvalidNameException>(() => NameValidator.EnsureValid(badName, "Organization"));
    }

    [Theory]
    [MemberData(nameof(ValidNames))]
    public void Validator_AcceptsValidNames(string goodName)
    {
        NameValidator.EnsureValid(goodName, "Organization");
        Assert.Null(NameValidator.Validate(goodName, "Organization"));
    }
}
