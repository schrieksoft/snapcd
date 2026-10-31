// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Nonsecured.Vaults;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Vaults;

/// <summary>
/// The SQL-backed vault against a real database, since what it stores is encrypted and the
/// round trip is the only thing that proves a value survives it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class SqlVaultTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private readonly List<string> _names = [];

    public SqlVaultTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_names.Count == 0) return;

        await using var db = _fixture.CreateDbContext();
        await db.Set<VaultSecret>().Where(v => _names.Contains(v.Name)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Written_Secret_Reads_Back()
    {
        var vault = NewVault();
        var name = NewName();

        await vault.SetSecretAsync(name, "hunter2");

        Assert.Equal("hunter2", await vault.GetSecretAsync(name));
    }

    /// <summary>
    /// The stored form is ciphertext. A test that only reads through the vault would pass just
    /// as well if the value were stored in the clear.
    /// </summary>
    [Fact]
    public async Task The_Stored_Form_Is_Not_The_Value()
    {
        var vault = NewVault();
        var name = NewName();

        await vault.SetSecretAsync(name, "hunter2");

        await using var db = _fixture.CreateDbContext();
        var row = await db.Set<VaultSecret>().AsNoTracking().FirstAsync(v => v.Name == name);

        Assert.DoesNotContain("hunter2", System.Text.Encoding.UTF8.GetString(row.Ciphertext));
    }

    [Fact]
    public async Task A_Missing_Secret_Throws()
    {
        var vault = NewVault();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => vault.GetSecretAsync(NewName()));
    }

    /// <summary>
    /// Every write is a new version, because the version stands for the secret wherever the value
    /// itself must not be carried.
    /// </summary>
    [Fact]
    public async Task Rewriting_A_Secret_Changes_Its_Version()
    {
        var vault = NewVault();
        var name = NewName();

        var first = await vault.SetSecretAsync(name, "one");
        var second = await vault.SetSecretAsync(name, "two");

        Assert.NotEqual(first, second);
        Assert.Equal("two", await vault.GetSecretAsync(name));
    }

    [Fact]
    public async Task SetIfChanged_Reports_An_Unchanged_Value_And_Keeps_Its_Version()
    {
        var vault = NewVault();
        var name = NewName();

        var written = await vault.SetIfChanged(name, "same");
        var again = await vault.SetIfChanged(name, "same");

        Assert.True(written.WasChanged);
        Assert.False(again.WasChanged);
        Assert.Equal(written.Version, again.Version);
    }

    [Fact]
    public async Task SetIfChanged_Writes_A_New_Version_When_The_Value_Differs()
    {
        var vault = NewVault();
        var name = NewName();

        var written = await vault.SetIfChanged(name, "before");
        var changed = await vault.SetIfChanged(name, "after");

        Assert.True(changed.WasChanged);
        Assert.NotEqual(written.Version, changed.Version);
        Assert.Equal("after", await vault.GetSecretAsync(name));
    }

    /// <summary>
    /// An empty value is refused rather than stored. The edit form sends the value field as it
    /// stands, so a save that never loaded the existing value would otherwise replace a real
    /// secret with nothing and report success.
    /// </summary>
    [Fact]
    public async Task SetIfChanged_Refuses_An_Empty_Value()
    {
        var vault = NewVault();
        var name = NewName();

        await vault.SetSecretAsync(name, "real value");

        await Assert.ThrowsAsync<ArgumentException>(() => vault.SetIfChanged(name, string.Empty));

        Assert.Equal("real value", await vault.GetSecretAsync(name));
    }

    [Fact]
    public async Task SetSecretAsync_Refuses_An_Empty_Value()
    {
        var vault = NewVault();

        await Assert.ThrowsAsync<ArgumentException>(() => vault.SetSecretAsync(NewName(), string.Empty));
    }

    [Fact]
    public async Task A_Deleted_Secret_Is_Gone()
    {
        var vault = NewVault();
        var name = NewName();

        await vault.SetSecretAsync(name, "value");
        await vault.DeleteSecretAsync(name);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => vault.GetSecretAsync(name));
    }

    /// <summary>
    /// Deleting something that is not there is how a best-effort cleanup behaves, so it must not
    /// throw: the caller is already past the point where it could react.
    /// </summary>
    [Fact]
    public async Task Deleting_A_Missing_Secret_Does_Not_Throw()
    {
        var vault = NewVault();

        await vault.DeleteSecretAsync(NewName());
    }

    /// <summary>
    /// Rows are removed outright here, so no name is ever recoverable. The Azure backend answers
    /// this differently, which is the whole reason the question is on IVault.
    /// </summary>
    [Fact]
    public async Task Nothing_Is_Ever_Soft_Deleted()
    {
        var vault = NewVault();
        var name = NewName();

        await vault.SetSecretAsync(name, "value");
        await vault.DeleteSecretAsync(name);

        Assert.False(await vault.IsSoftDeletedAsync(name));
    }

    /// <summary>
    /// A name freed by a delete can be written again. On the Azure backend this is the case that
    /// needs the soft-deleted name recovered first.
    /// </summary>
    [Fact]
    public async Task A_Deleted_Name_Can_Be_Written_Again()
    {
        var vault = NewVault();
        var name = NewName();

        await vault.SetSecretAsync(name, "first");
        await vault.DeleteSecretAsync(name);
        await vault.SetSecretAsync(name, "second");

        Assert.Equal("second", await vault.GetSecretAsync(name));
    }

    /// <summary>
    /// Listing by prefix is how a deleted Stack, Namespace or Module has its values found, so it
    /// must match exactly what belongs to that owner and nothing adjacent.
    /// </summary>
    [Fact]
    public async Task Names_Are_Listed_By_Prefix()
    {
        var vault = NewVault();
        var owner = Guid.NewGuid().ToString("N");
        var mine = $"test--{owner}--one";
        var alsoMine = $"test--{owner}--two";
        var somebodyElses = $"test--{Guid.NewGuid():N}--one";

        foreach (var name in new[] { mine, alsoMine, somebodyElses })
        {
            _names.Add(name);
            await vault.SetSecretAsync(name, "value");
        }

        var found = await vault.ListNamesAsync($"test--{owner}--");

        Assert.Equal(2, found.Count);
        Assert.Contains(mine, found);
        Assert.Contains(alsoMine, found);
        Assert.DoesNotContain(somebodyElses, found);
    }

    [Fact]
    public async Task Listing_A_Prefix_Nothing_Matches_Returns_Nothing()
    {
        var vault = NewVault();

        Assert.Empty(await vault.ListNamesAsync($"test--{Guid.NewGuid():N}--"));
    }

    private SqlVault NewVault() =>
        new(new FixtureDbContextFactory(_fixture),
            new byte[32],
            NullLogger<SqlVault>.Instance);

    private string NewName()
    {
        var name = $"test--{Guid.NewGuid():N}";
        _names.Add(name);
        return name;
    }

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }
}
