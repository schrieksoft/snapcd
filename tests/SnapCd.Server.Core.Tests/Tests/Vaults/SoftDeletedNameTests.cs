// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Services.Interfaces.Vaults;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Vaults;

/// <summary>
/// The rule the secret migrator uses to decide whether a destination already holds a name.
///
/// A name in the deleted-but-recoverable state reads as absent through GetSecretAsync while still
/// refusing a write, so deciding by absence alone plans a copy that cannot run. These tests drive
/// the rule against a vault that reports that state, which the Azure backend does and the SQL one
/// never does.
/// </summary>
public class SoftDeletedNameTests
{
    /// <summary>The migrator's rule, as SecretMigratorService applies it when probing a destination.</summary>
    private static async Task<bool> DestinationHolds(IVault vault, string name) =>
        await TryGet(vault, name) || await vault.IsSoftDeletedAsync(name);

    private static async Task<bool> TryGet(IVault vault, string name)
    {
        try
        {
            _ = await vault.GetSecretAsync(name);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task A_Live_Name_Is_Held()
    {
        var vault = new FakeVault { Live = { "a" } };

        Assert.True(await DestinationHolds(vault, "a"));
    }

    [Fact]
    public async Task An_Absent_Name_Is_Not_Held()
    {
        var vault = new FakeVault();

        Assert.False(await DestinationHolds(vault, "a"));
    }

    /// <summary>
    /// The case this rule exists for. Reading alone says the name is free, and a copy planned on
    /// that basis fails at execution because the write is refused.
    /// </summary>
    [Fact]
    public async Task A_Soft_Deleted_Name_Is_Held_Even_Though_It_Reads_As_Absent()
    {
        var vault = new FakeVault { SoftDeleted = { "a" } };

        Assert.False(await TryGet(vault, "a"));
        Assert.True(await DestinationHolds(vault, "a"));
    }

    private sealed class FakeVault : IVault
    {
        public HashSet<string> Live { get; } = [];
        public HashSet<string> SoftDeleted { get; } = [];

        public Task<string> GetSecretAsync(string secretName) =>
            Live.Contains(secretName)
                ? Task.FromResult("value")
                : throw new KeyNotFoundException(secretName);

        public Task<bool> IsSoftDeletedAsync(string secretName, CancellationToken ct = default) =>
            Task.FromResult(SoftDeleted.Contains(secretName));

        public Task<SetIfChangedResult> SetIfChanged(string secretName, string value) =>
            throw new NotSupportedException();

        public Task<VersionedSecret> GetSecretWithVersionAsync(string secretName) =>
            throw new NotSupportedException();

        public Task<string> SetSecretAsync(string secretName, string value) =>
            throw new NotSupportedException();

        public Task DeleteSecretAsync(string secretName) => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListNamesAsync(string prefix, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Dispose() { }
    }
}
