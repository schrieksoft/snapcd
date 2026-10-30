// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Services.Interfaces.Vaults;
using SnapCd.Server.Core.Services.Utils.CallerContext;
using SnapCd.Server.Core.Services.Utils.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Nonsecured.Vaults;

/// <summary>
/// Moves Secret values onto the owner-scoped naming scheme, one Secret at a time.
///
/// A value is read under the name its row records, written under the new name, and the old one
/// deleted; the row is updated last, so a run that stops part way leaves the value found rather
/// than lost. Re-running costs a lookup per Secret and changes nothing else.
/// </summary>
public class SecretNameSweepJob(
    SnapCdDbContext dbContext,
    IVaultFactory vaultFactory,
    IOptions<SecretStoreSettings> secretStoreSettings,
    ILogger<SecretNameSweepJob> logger)
{
    private readonly SecretStoreSettings _settings = secretStoreSettings.Value;

    public async Task ExecuteJob()
    {
        using var _ = CallerContext.Begin(CallerKind.System);

        var pending = await dbContext.Secrets
            .Where(s => !s.NameContainsScope)
            .OrderBy(s => s.CreatedDateTime)
            .Take(BatchSize)
            .ToListAsync();

        if (pending.Count == 0) return;

        logger.LogInformation("Secret name sweep: {Count} Secret(s) to move", pending.Count);

        var vaultUrls = await VaultUrlsByOrganization(pending);

        var moved = 0;
        var missing = 0;

        foreach (var secret in pending)
        {
            try
            {
                if (await Sweep(secret, vaultUrls[secret.OrganizationId])) moved++;
                else missing++;
            }
            catch (Exception ex)
            {
                // One Secret failing says nothing about the next, and the next run retries this one.
                logger.LogError(ex, "Secret name sweep: Secret {SecretId} could not be moved", secret.Id);
            }
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Secret name sweep: {Moved} moved, {Missing} with no value in the vault", moved, missing);
    }

    /// <summary>
    /// Returns whether the Secret now has a value under the owner-scoped name. The three cases are
    /// the old name holding it, the new name already holding it, and neither.
    /// </summary>
    private async Task<bool> Sweep(Secret secret, string vaultUrl)
    {
        using var vault = vaultFactory.Create(vaultUrl);

        var scopeless = RemoteSecretName.Scopeless(secret, secret.OrganizationId);
        var scoped = RemoteSecretName.Scoped(secret, secret.OrganizationId);

        var value = await Read(vault, scopeless);
        if (value != null)
        {
            await vault.SetSecretAsync(scoped, value);
            await vault.DeleteSecretAsync(scopeless);

            secret.NameContainsScope = true;
            secret.RemoteValueMissingSince = null;
            return true;
        }

        // An earlier run wrote the new name and stopped before the row caught up.
        if (await Read(vault, scoped) != null)
        {
            secret.NameContainsScope = true;
            secret.RemoteValueMissingSince = null;
            return true;
        }

        // Under neither name. The flag is left alone: this Secret is not moved, it is broken, and
        // marking it moved would hide that.
        if (secret.RemoteValueMissingSince == null)
        {
            secret.RemoteValueMissingSince = DateTime.UtcNow;
            logger.LogWarning(
                "Secret {SecretId} in organization {OrganizationId} has no value in the vault under either name",
                secret.Id, secret.OrganizationId);
        }

        return false;
    }

    private static async Task<string?> Read(IVault vault, string name)
    {
        try
        {
            return await vault.GetSecretAsync(name);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The input vault each organization uses, resolved once per run rather than per Secret.
    /// </summary>
    private async Task<Dictionary<Guid, string>> VaultUrlsByOrganization(List<Secret> secrets)
    {
        var organizationIds = secrets.Select(s => s.OrganizationId).Distinct().ToList();

        var configured = await dbContext.Organizations
            .Where(o => organizationIds.Contains(o.Id))
            .Select(o => new { o.Id, o.InputKeyVaultUrl })
            .ToListAsync();

        return configured.ToDictionary(
            o => o.Id,
            o => o.InputKeyVaultUrl ?? _settings.AzureKeyVault.DefaultInputKeyVaultUrl);
    }

    /// <summary>
    /// How many Secrets one run moves. On Azure each is several network calls, so a run is kept
    /// short and the next one picks up where this left off.
    /// </summary>
    private const int BatchSize = 200;
}
