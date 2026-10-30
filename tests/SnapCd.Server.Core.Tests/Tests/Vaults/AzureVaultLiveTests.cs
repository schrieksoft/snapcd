// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Server.Core.Services.Utils.Vaults;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Vaults;

/// <summary>
/// The Azure backend against a real Key Vault. Soft delete and the recovery that follows it are
/// service behaviour, so nothing short of the real service tells you whether they work.
///
/// Skipped unless SNAPCD_LIVE_AKV_URL names a vault with soft delete on and purge protection off,
/// because it needs an Azure sign-in, writes to that vault, and takes about a minute: delete and
/// recover are long-running operations there.
/// </summary>
public class AzureVaultLiveTests
{
    private const string VaultUrlVariable = "SNAPCD_LIVE_AKV_URL";

    private static string? VaultUrl => Environment.GetEnvironmentVariable(VaultUrlVariable);

    [Fact]
    public async Task A_Deleted_Secret_Is_Recoverable_Rather_Than_Purged()
    {
        var vault = NewVault();
        if (vault is null) return;

        var name = NewName();

        await vault.SetSecretAsync(name, "before-delete");
        await vault.DeleteSecretAsync(name);

        try
        {
            // Reads as absent, which is why absence alone is not enough to call the name free.
            await Assert.ThrowsAnyAsync<Exception>(() => vault.GetSecretAsync(name));
            Assert.True(await vault.IsSoftDeletedAsync(name));
        }
        finally
        {
            await Purge(name);
        }
    }

    /// <summary>
    /// Writing a name that is held in the deleted state. Key Vault refuses it until the name is
    /// recovered, which is what SetSecretAsync now does on its own.
    /// </summary>
    [Fact]
    public async Task Writing_A_Soft_Deleted_Name_Recovers_It_And_Succeeds()
    {
        var vault = NewVault();
        if (vault is null) return;

        var name = NewName();

        await vault.SetSecretAsync(name, "first");
        await vault.DeleteSecretAsync(name);
        Assert.True(await vault.IsSoftDeletedAsync(name));

        try
        {
            await vault.SetSecretAsync(name, "second");

            Assert.Equal("second", await vault.GetSecretAsync(name));
            Assert.False(await vault.IsSoftDeletedAsync(name));
        }
        finally
        {
            await Purge(name);
        }
    }

    [Fact]
    public async Task A_Name_That_Was_Never_Deleted_Is_Not_Soft_Deleted()
    {
        var vault = NewVault();
        if (vault is null) return;

        Assert.False(await vault.IsSoftDeletedAsync(NewName()));
    }

    [Fact]
    public async Task SetIfChanged_Reports_An_Unchanged_Value()
    {
        var vault = NewVault();
        if (vault is null) return;

        var name = NewName();

        try
        {
            var written = await vault.SetIfChanged(name, "same");
            var again = await vault.SetIfChanged(name, "same");

            Assert.True(written.WasChanged);
            Assert.False(again.WasChanged);
            Assert.Equal(written.Version, again.Version);
        }
        finally
        {
            await Purge(name);
        }
    }

    /// <summary>Null when the variable is unset, which every test here treats as "do not run".</summary>
    private static AzureVault? NewVault() =>
        string.IsNullOrWhiteSpace(VaultUrl)
            ? null
            : new AzureVault(VaultUrl!, new DefaultAzureCredential(), NullLogger<AzureVault>.Instance);

    private static string NewName() => $"livetest-{Guid.NewGuid():N}";

    /// <summary>
    /// Leaves the vault as the test found it. The name may be live or already soft-deleted, so
    /// this deletes first and ignores the delete failing, then purges.
    /// </summary>
    private static async Task Purge(string name)
    {
        var client = new SecretClient(new Uri(VaultUrl!), new DefaultAzureCredential());

        try
        {
            var deletion = await client.StartDeleteSecretAsync(name);
            await deletion.WaitForCompletionAsync();
        }
        catch (RequestFailedException)
        {
            // Not live: already deleted, or never written.
        }

        try
        {
            await client.PurgeDeletedSecretAsync(name);
        }
        catch (RequestFailedException)
        {
            // Already purged, or the vault refuses a purge. Neither is the test's concern.
        }
    }
}
