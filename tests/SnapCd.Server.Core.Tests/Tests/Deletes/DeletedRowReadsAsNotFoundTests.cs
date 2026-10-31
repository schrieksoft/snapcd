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
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Deletes;

/// <summary>
/// Reading a row that has been deleted says it is gone, rather than that the caller may not see
/// it. A client holding an id of its own cannot otherwise tell the two apart, and Terraform reads
/// a permission error as a failure where it would treat an absence as a resource to recreate.
///
/// The distinction is drawn only inside the organization the caller is authenticated against, so
/// it says nothing about ids belonging to any other.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class DeletedRowReadsAsNotFoundTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private readonly List<Guid> _stacks = [];

    public DeletedRowReadsAsNotFoundTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.Stacks.Where(s => _stacks.Contains(s.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Deleted_Stack_Reads_As_Not_Found()
    {
        var stackId = await SeedStack();

        await using (var db = _fixture.CreateDbContext())
        {
            await db.Stacks.Where(s => s.Id == stackId).ExecuteDeleteAsync();
        }

        using var repository = Owned();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => repository.Get(stackId, _organizationId));
    }

    /// <summary>
    /// An id that never existed is the same case: nothing is there, and saying so reveals nothing
    /// the caller did not already supply.
    /// </summary>
    [Fact]
    public async Task An_Id_That_Never_Existed_Reads_As_Not_Found()
    {
        using var repository = Owned();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => repository.Get(Guid.NewGuid(), _organizationId));
    }

    /// <summary>
    /// A row that is there but not readable still reports a permission problem, which is what
    /// keeps the two answers apart from each other rather than collapsing both into "gone".
    /// </summary>
    [Fact]
    public async Task A_Stack_The_Principal_Cannot_Read_Still_Refuses_On_Permission()
    {
        var stackId = await SeedStack();

        using var repository = Unprivileged();

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => repository.Get(stackId, _organizationId));
    }

    [Fact]
    public async Task A_Stack_That_Is_There_And_Readable_Is_Returned()
    {
        var stackId = await SeedStack();

        using var repository = Owned();

        var stack = await repository.Get(stackId, _organizationId);

        Assert.Equal(stackId, stack.Id);
    }

    private StackSecuredRepository Owned() =>
        Secured(_fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);

    /// <summary>A principal of the organization holding no role that grants a read.</summary>
    private StackSecuredRepository Unprivileged() => Secured(Guid.NewGuid());

    private StackSecuredRepository Secured(Guid principalId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            principalId, PrincipalDiscriminator.User, _organizationId);

        return new StackSecuredRepository(
            new StackRepository(
                _fixture.CreateDbContext(),
                principalProvider,
                _fixture.CreateMockBus(),
                Options.Create(new StackRepositorySettings())),
            principalProvider);
    }

    private async Task<Guid> SeedStack()
    {
        var stack = new Stack
        {
            Id = Guid.NewGuid(),
            Name = $"deleted-read-{Guid.NewGuid():N}"[..24],
            OrganizationId = _organizationId,
            CreatedDateTime = DateTime.UtcNow
        };

        _stacks.Add(stack.Id);

        await using var db = _fixture.CreateDbContext();
        db.Stacks.Add(stack);
        await db.SaveChangesAsync();

        return stack.Id;
    }
}
